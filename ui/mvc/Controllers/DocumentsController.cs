using Azure.Search.Documents.Models;
using Azure.Search.Documents;
using Azure;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;
using Microsoft.Azure.Cosmos;
using PhiDeidPortal.Ui;
using PhiDeidPortal.Ui.Services;
using PhiDeidPortal.Ui.Entities;
using Microsoft.AspNetCore.Razor.TagHelpers;
using PhiDeidPortal.Ui.Common;
using System;
using System.Net;
using System.Reflection.Metadata;

namespace PhiDeidPortal.Ui.Controllers
{
    [ApiController]
    [Authorize]
    public class DocumentsController : ControllerBase
    {
        private readonly IBlobService _blobService;
        private readonly ICosmosService _cosmosService;
        private readonly IConfigurationSection _storageConfiguration;
        private readonly IAISearchService _searchService;

        private readonly string _containerName = "";


        public DocumentsController(IBlobService blobService, IConfiguration configuration, CosmosClient cosmosClient, IAISearchService searchService, ICosmosService cosmosService)
        {
            _blobService = blobService;
            _storageConfiguration = configuration.GetSection("StorageAccount");
            _cosmosService = cosmosService;
            _searchService = searchService;

            _containerName = $"{_storageConfiguration["Container"]}";
        }

        [HttpGet]
        [Route("api/documents/{filename}")]
        public async Task<IActionResult> Get(string filename)
        {
            return new FileStreamResult(
                await _blobService.GetDocumentStreamAsync(_containerName, filename),
                "application/octet-stream");
        }

        [Route("api/documents/upload")]
        public async Task<IActionResult> Post(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded");
            }

            string blobName = $"{Path.GetFileName(file.FileName)}";

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
                uri = await _blobService.UploadDocumentAsync(file, _containerName);
            }
            catch (Exception ex)
            {
                return BadRequest($"Blob storage upsert failed: {Environment.NewLine} {ex.Message}");
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
                JustificationText: ""
                );

                await _cosmosService.UpsertMetadataRecord(metadataRecord);
            }
            catch (Exception ex)
            {
                return BadRequest($"Cosmos upsert failed: {Environment.NewLine} {ex.Message}");
            }

            return Ok("Document uploaded successfully");
        }

        [HttpPost]
        [Route("api/documents/reset")]
        public async Task<IActionResult> Reset(StatusChangeRequestEntity document)
        {
            // todo update cosmos with document.Id and document.Message
            var reset = await _searchService.ResetDocument(document.Key);
            if (!reset) return BadRequest("Reset document failed.");
            var reindex = await _searchService.RunIndexer(String.Empty);
            if (!reindex) return BadRequest("Reindex failed.");

            return Ok();
        }

        [HttpPost]
        [Route("api/documents/delete")]
        public async Task<IActionResult> Delete(DeleteDocumentRequestEntity document)
        {
            var cosmosDocument = GetMetadataRecordByAuthorAndUri(document.Uri);
            if (cosmosDocument is null) return BadRequest("Document not found for the given author.");
            var deleteBlob = await _blobService.DeleteDocumentAsync(_containerName, document.Uri);
            if (!deleteBlob.Value) return BadRequest("Reset document failed - Storage.");
            var deleteCosmos = await _cosmosService.DeleteMetadataRecord(cosmosDocument);
            if (deleteCosmos.StatusCode != HttpStatusCode.NoContent) return BadRequest("Reset document failed - Cosmos.");
            var deleteIndex = await _searchService.DeleteDocument(document.Key);
            if (!deleteIndex) return BadRequest("Reset document failed - Index.");

            return Ok();
        }

        [HttpPost]
        [Route("api/documents/approve")]
        public async Task<IActionResult> Approve(StatusChangeRequestEntity document)
        {
            var existingMetadataRecord = GetMetadataRecordByAuthorAndUri(document.Uri);
            if (existingMetadataRecord is null) return BadRequest("Document not found for the given author.");

            MetadataRecord newMetadataRecord = new(
                id: existingMetadataRecord.id,
                FileName: existingMetadataRecord.FileName,
                Uri: existingMetadataRecord.Uri,
                Author: existingMetadataRecord.Author,
                Status: (int) DeidStatus.Approved,
                OrganizationalMetadata: existingMetadataRecord.OrganizationalMetadata,
                LastIndexed: existingMetadataRecord.LastIndexed,
                JustificationText: existingMetadataRecord.JustificationText
                );
            
            await _cosmosService.UpsertMetadataRecord(newMetadataRecord);

            // todo update cosmos with document.Id and document.Message
            var reset = await _searchService.ResetDocument(document.Key);
            if (!reset) return BadRequest("Reset document failed.");
            var reindex = await _searchService.RunIndexer(String.Empty);
            if (!reindex) return BadRequest("Reindex failed.");

            return Ok();
        }

        [HttpPost]
        [Route("api/documents/deny")]
        public async Task<IActionResult> Deny(StatusChangeRequestEntity document)
        {
            var existingMetadataRecord = GetMetadataRecordByAuthorAndUri(document.Uri);
            if (existingMetadataRecord is null) return BadRequest("Document not found for the given author.");

            MetadataRecord newMetadataRecord = new(
                id: existingMetadataRecord.id,
                FileName: existingMetadataRecord.FileName,
                Uri: existingMetadataRecord.Uri,
                Author: existingMetadataRecord.Author,
                Status: (int)DeidStatus.Denied,
                OrganizationalMetadata: existingMetadataRecord.OrganizationalMetadata,
                LastIndexed: existingMetadataRecord.LastIndexed,
                JustificationText: existingMetadataRecord.JustificationText
                );

            await _cosmosService.UpsertMetadataRecord(newMetadataRecord);

            // todo update cosmos with document.Id and document.Message
            var reset = await _searchService.ResetDocument(document.Key);
            if (!reset) return BadRequest("Reset document failed.");
            var reindex = await _searchService.RunIndexer(String.Empty);
            if (!reindex) return BadRequest("Reindex failed.");

            return Ok();
        }

        [HttpPost]
        [Route("api/documents/justify")]
        public async Task<IActionResult> SubmitJustification(StatusChangeRequestEntity document)
        {
            var existingMetadataRecord = GetMetadataRecordByAuthorAndUri(document.Uri);
            if (existingMetadataRecord is null) return BadRequest("Document not found for the given author.");

            MetadataRecord newMetadataRecord = new(
                id: existingMetadataRecord.id,
                FileName: existingMetadataRecord.FileName,
                Uri: existingMetadataRecord.Uri,
                Author: existingMetadataRecord.Author,
                Status: (int)DeidStatus.JustificationApprovalPending,
                OrganizationalMetadata: existingMetadataRecord.OrganizationalMetadata,
                LastIndexed: existingMetadataRecord.LastIndexed,
                JustificationText: document.Comment ??= ""
                );

            await _cosmosService.UpsertMetadataRecord(newMetadataRecord);

            var reset = await _searchService.ResetDocument(document.Key);
            if (!reset) return BadRequest("Reset document failed.");
            var reindex = await _searchService.RunIndexer(String.Empty);
            if (!reindex) return BadRequest("Reindex failed.");

            return Ok();
        }

        private MetadataRecord? GetMetadataRecordByAuthorAndUri(string uri)
        {
            var user = User.Identity?.Name;
            if (user is null) return null;
            var cosmosDocument = _cosmosService.GetMetadataRecordByAuthorAndUri(user, uri);
            return cosmosDocument;
        }
    }
}
