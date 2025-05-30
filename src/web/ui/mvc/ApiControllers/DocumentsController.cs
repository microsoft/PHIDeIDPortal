﻿using Azure.Search.Documents.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.FeatureManagement.Mvc;
using PhiDeidPortal.Ui.Entities;
using PhiDeidPortal.Ui.Services;
using System.Linq;
using System.Net;
using System.Reflection.Metadata;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace PhiDeidPortal.Ui.Controllers
{
    [ApiController]
    [Authorize]
    public class DocumentsController : ControllerBase
    {
        private readonly IBlobService _blobService;
        private readonly ICosmosService _cosmosService;
        private readonly IConfigurationSection _storageConfiguration;
        private readonly IConfigurationSection _searchConfiguration;
        private readonly IAISearchService _searchService;
        private readonly Services.IAuthorizationService _authorizationService;
        private readonly IFeatureService _featureService;

        private readonly string _containerName = "";

        public DocumentsController(IBlobService blobService, IConfiguration configuration, CosmosClient cosmosClient, IAISearchService searchService, ICosmosService cosmosService, Services.IAuthorizationService authorizationService, IFeatureService featureService)
        {
            _blobService = blobService;
            _storageConfiguration = configuration.GetSection("StorageAccount");
            _searchConfiguration = configuration.GetSection("SearchService");
            _cosmosService = cosmosService;
            _searchService = searchService;
            _authorizationService = authorizationService;
            _featureService = featureService;

            _containerName = $"{_storageConfiguration["Container"]}";
        }

        [HttpGet]
        [Route("api/documents/{filename}")]
        [FeatureGate(Feature.Download)]
        public async Task<IActionResult> Get(string filename)
        {
            return new FileStreamResult(
                await _blobService.GetDocumentStreamAsync(_containerName, filename),
                "application/octet-stream");
        }

        [Route("api/documents/upload")]
        [FeatureGate(Feature.Upload)]
        public async Task<IActionResult> Post(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("No file uploaded");

            if (!AllowableContentType.IsAllowable(file.ContentType)) return BadRequest($"{file.ContentType} not supported.");

            string blobName = GetFileNameWithTime(Regex.Replace(Path.GetFileName(file.FileName), @"[^a-zA-Z0-9_\-\.]", ""));       

            string tags = this.HttpContext.Request.Form["uploadTags"];
            tags ??= "";

            var organizationalMetadata = new List<string>();
            foreach (var s in tags.Split(','))
            {
                organizationalMetadata.Add(s);
            }

            string uri = "";
            try
            {
                uri = await _blobService.UploadDocumentAsync(file, _containerName, blobName);
                if (String.IsNullOrWhiteSpace(uri)) { throw new Exception(); }
            }
            catch
            {
                return StatusCode(500, "Cannot connect to the storage account.");
            }

            try
            {
                MetadataRecord metadataRecord = new(
                id: Guid.NewGuid().ToString(),
                FileName: blobName,
                Uri: uri,
                Author: User.Identity.Name,
                Status: 1,
                OrganizationalMetadata: organizationalMetadata.ToArray(),
                LastIndexed: DateTime.MinValue,
                AwaitingIndex: true,
                JustificationText: ""
                );

                var upload = await _cosmosService.UpsertMetadataRecordAsync(metadataRecord);
                if (upload.StatusCode !=  HttpStatusCode.Created) { throw new Exception(); }
            }
            catch (Exception ex)
            {
                return BadRequest($"Cannot upload document metadata to the database.");
            }

            var reupload = _searchConfiguration["ReindexOnUpload"] ?? "false";
            if (reupload.Equals("true", StringComparison.CurrentCultureIgnoreCase)) { await _searchService.RunIndexerAsync(String.Empty); }

            return Ok("Document uploaded successfully");
        }

        [HttpPost]
        [Route("api/documents/reset")]
        [FeatureGate(Feature.ResetIndex)]
        public async Task<IActionResult> Reset(StatusChangeRequestEntity document)
        {
            var reset = await ResetDocumentAsync(document.Uri);
            if (!String.IsNullOrWhiteSpace(reset)) return BadRequest(reset);

            return Ok();
        }

        [HttpPost]
        [Route("api/documents/delete")]
        [FeatureGate(Feature.Delete)]
        public async Task<IActionResult> Delete(DeleteDocumentRequestEntity document)
        {
            var cosmosDocument = GetMetadataRecordByUri(document.Uri);
            if (cosmosDocument is null) return BadRequest("Document deleted with errors. Document metadata not found for the given author.");
            var deleteCosmos = await _cosmosService.DeleteMetadataRecordAsync(cosmosDocument);
            if (!deleteCosmos.IsSuccess) return BadRequest("Document deleted with errors. Metadata database failure.");

            if (String.IsNullOrWhiteSpace(document.Uri)) return BadRequest("Document could not be deleted. Invalid document URI.");
            var deleteBlob = await _blobService.DeleteDocumentAsync(_containerName, document.Uri);
            if (!deleteBlob.IsSuccess) return BadRequest("Document could not be deleted. Storage account failure.");

            var searchDocument = _searchService.SearchAsync($"metadata_storage_path eq '{document.Uri}'").Result.FirstOrDefault();
            if (searchDocument is null) return BadRequest("Document deleted with errors. Document not found in the search index.");
            var searchKey = searchDocument.Document["id"]?.ToString();
            if (searchKey is null) return BadRequest("Document deleted with errors. Document key not found in the search index.");
            var deleteIndex = await _searchService.DeleteDocumentAsync(searchKey);
            if (!deleteIndex.IsSuccess) return BadRequest("Document deleted with errors. Index deletion failure.");

            return Ok();
        }

        [HttpPost]
        [Route("api/documents/reindex")]
        [FeatureGate(Feature.Reindex)]
        public async Task<IActionResult> Reindex()
        {
            // Admins only
            if (!_authorizationService.HasElevatedRights(User)) return Unauthorized("Unauthorized. Please request access to the app administrator group.");
            var reindex = await _searchService.RunIndexerAsync(String.Empty);
            if (!reindex.IsSuccess) return BadRequest("Delete document failed.");

            return Ok();
        }

        [HttpPost]
        [Route("api/documents/approve")]
        [FeatureGate(Feature.ManualReviewView)]
        public async Task<IActionResult> Approve(StatusChangeRequestEntity document)
        {
            return await HandleApproval(document, DeidStatus.Approved);
        }
        
        [HttpPost]
        [Route("api/documents/deny")]
        [FeatureGate(Feature.ManualReviewView)]
        public async Task<IActionResult> Deny(StatusChangeRequestEntity document)
        {
            return await HandleApproval(document, DeidStatus.Denied);
        }

        [HttpPost]
        [Route("api/documents/justify")]
        [FeatureGate(Feature.JustificationView)]
        public async Task<IActionResult> SubmitJustification(StatusChangeRequestEntity document)
        {
            var oldRecord = GetMetadataRecordByUri(document.Uri);
            if (oldRecord is null) return BadRequest("Document not found for the given author.");
            if (oldRecord.AwaitingIndex) return Conflict("Document is awaiting reindex. Please refresh and try again.");
            if (oldRecord.Status > 2) return Conflict("Document is awaiting reindex. Please refresh and try again.");

            MetadataRecord newRecord = new(
                id: Guid.NewGuid().ToString(),
                FileName: oldRecord.FileName,
                Uri: oldRecord.Uri,
                Author: oldRecord.Author,
                Status: (int)DeidStatus.JustificationApprovalPending,
                OrganizationalMetadata: oldRecord.OrganizationalMetadata,
                LastIndexed: oldRecord.LastIndexed,
                AwaitingIndex: true,
                JustificationText: document.Comment ??= ""
                );

            await _cosmosService.DeleteMetadataRecordAsync(oldRecord);
            await _cosmosService.UpsertMetadataRecordAsync(newRecord);

            var reset = await ResetDocumentAsync(document.Uri);
            if (!String.IsNullOrWhiteSpace(reset)) return BadRequest(reset);

            return Ok();
        }

        private async Task<IActionResult> HandleApproval(StatusChangeRequestEntity document, DeidStatus status)
        {
            var oldRecord = GetMetadataRecordByUri(document.Uri, true);
            if (oldRecord is null) return BadRequest("Document not found for the given approver.");
            if (oldRecord.AwaitingIndex) return Conflict("Document is awaiting reindex. Please refresh and try again.");
            if (oldRecord.Status > 3) return Conflict("Document is awaiting reindex. Please refresh and try again.");

            MetadataRecord newRecord = new(
                id: Guid.NewGuid().ToString(),
                FileName: oldRecord.FileName,
                Uri: oldRecord.Uri,
                Author: oldRecord.Author,
                Status: (int)status,
                OrganizationalMetadata: oldRecord.OrganizationalMetadata,
                LastIndexed: oldRecord.LastIndexed,
                AwaitingIndex: true,
                JustificationText: oldRecord.JustificationText
                );

            await _cosmosService.DeleteMetadataRecordAsync(oldRecord);
            await _cosmosService.UpsertMetadataRecordAsync(newRecord);

            var reset = await ResetDocumentAsync(document.Uri);
            if (!String.IsNullOrWhiteSpace(reset)) return BadRequest(reset);

            return Ok();
        }

        private MetadataRecord? GetMetadataRecordByUri(string uri, bool adminOnly = false)
        {
            var user = User.Identity?.Name;
            if (user is null) return null;
            if (adminOnly && !_authorizationService.HasElevatedRights(User)) return null;
            if (adminOnly) return _cosmosService.GetMetadataRecordByUri(uri);
            return _cosmosService.GetMetadataRecordByUriAndAuthor(uri,user);
        }

        private string GetFileNameWithTime(string filename)
        {          
            if (String.IsNullOrWhiteSpace(filename)) return filename;

            var n = Path.GetFileNameWithoutExtension(filename); 
            var e = Path.GetExtension(filename);
            var t = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

            return $"{n}-{t}{e}";
        }

        private async Task<string> ResetDocumentAsync(string uri)
        {
            var searchDocument = _searchService.SearchAsync($"metadata_storage_path eq '{uri}'").Result.FirstOrDefault();
            if (searchDocument is null) return "Reset document failed. Document not found in the search index.";
            var searchKey = searchDocument.Document["id"]?.ToString();
            if (searchKey is null) return "Reset document failed. Document key not found in the search index.";
            var reset = await _searchService.ResetDocumentAsync(searchKey);
            if (!reset.IsSuccess) return $"Reset document failed. Code {reset.Code}";
            var reindex = await _searchService.RunIndexerAsync(String.Empty);
            if (!reindex.IsSuccess) return $"Reindex failed. Code {reindex.Code}";
            return string.Empty;
        }

    }
}
