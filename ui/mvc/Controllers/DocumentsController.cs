﻿using Azure.Search.Documents.Models;
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
        private readonly IConfigurationSection _searchConfiguration;
        private readonly IAISearchService _searchService;
        private readonly Services.IAuthorizationService _authorizationService;

        private readonly string _containerName = "";


        public DocumentsController(IBlobService blobService, IConfiguration configuration, CosmosClient cosmosClient, IAISearchService searchService, ICosmosService cosmosService, Services.IAuthorizationService authorizationService)
        {
            _blobService = blobService;
            _storageConfiguration = configuration.GetSection("StorageAccount");
            _searchConfiguration = configuration.GetSection("SearchService");
            _cosmosService = cosmosService;
            _searchService = searchService;
            _authorizationService = authorizationService;

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
                if (String.IsNullOrWhiteSpace(uri)) { throw new Exception(); }
            }
            catch
            {
                return StatusCode(500, "Error uploading document to the Storage Account.");
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

                var upload = await _cosmosService.UpsertMetadataRecord(metadataRecord);
                if (upload.StatusCode !=  HttpStatusCode.Created) { throw new Exception(); }
            }
            catch
            {
                return BadRequest($"Error uploading document metadata to Cosmos DB.");
            }

            var reupload = _searchConfiguration["ReindexOnUpload"] ?? "false";
            if (reupload.Equals("true", StringComparison.CurrentCultureIgnoreCase)) { await _searchService.RunIndexer(String.Empty); }

            return Ok("Document uploaded successfully");
        }

        [HttpPost]
        [Route("api/documents/reset")]
        public async Task<IActionResult> Reset(StatusChangeRequestEntity document)
        {
            // todo update cosmos with document.Id and document.Message
            var reset = await _searchService.ResetDocument(document.Key);
            if (!reset.IsSuccess) return BadRequest($"Reset document failed. Code {reset.Code}");
            var reindex = await _searchService.RunIndexer(String.Empty);
            if (!reindex.IsSuccess) return BadRequest($"Reindex failed. Code {reindex.Code}");

            return Ok();
        }

        [HttpPost]
        [Route("api/documents/delete")]
        public async Task<IActionResult> Delete(DeleteDocumentRequestEntity document)
        {
            var cosmosDocument = GetMetadataRecordByUri(document.Uri);
            if (cosmosDocument is null) return BadRequest("Document not found for the given author.");
            var deleteBlob = await _blobService.DeleteDocumentAsync(_containerName, document.Uri);
            if (!deleteBlob.IsSuccess) return BadRequest("Delete document failed - Storage (1).");
            var deleteCosmos = await _cosmosService.DeleteMetadataRecord(cosmosDocument);
            if (!deleteCosmos.IsSuccess) return BadRequest("Delete document failed - Cosmos (2).");
            var deleteIndex = await _searchService.DeleteDocument(document.Key);
            if (!deleteIndex.IsSuccess) return BadRequest("Delete document failed - Index (3).");

            return Ok();
        }

        [HttpPost]
        [Route("api/documents/deletefromsearchindex")]
        public async Task<IActionResult> DeleteFromSearchIndex(DeleteDocumentRequestEntity document)
        {
            // Admins only
            if (!_authorizationService.Authorize(User)) return Unauthorized("Unauthorized. Please request access to the app administrator group.");
            var deleteIndex = await _searchService.DeleteDocument(document.Key);
            if (!deleteIndex.IsSuccess) return BadRequest("Delete document failed.");

            return Ok();
        }

        [HttpPost]
        [Route("api/documents/reindex")]
        public async Task<IActionResult> Reindex()
        {
            // Admins only
            if (!_authorizationService.Authorize(User)) return Unauthorized("Unauthorized. Please request access to the app administrator group.");
            var reindex = await _searchService.RunIndexer(String.Empty);
            if (!reindex.IsSuccess) return BadRequest("Delete document failed.");

            return Ok();
        }

        [HttpPost]
        [Route("api/documents/approve")]
        public async Task<IActionResult> Approve(StatusChangeRequestEntity document)
        {
            var existingMetadataRecord = GetMetadataRecordByUri(document.Uri, true);
            if (existingMetadataRecord is null) return BadRequest("Document not found for the given approver.");

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
            if (!reset.IsSuccess) return BadRequest($"Reset document failed. Code {reset.Code}");
            var reindex = await _searchService.RunIndexer(String.Empty);
            if (!reindex.IsSuccess) return BadRequest($"Reindex failed. Code {reindex.Code}");

            return Ok();
        }

        [HttpPost]
        [Route("api/documents/deny")]
        public async Task<IActionResult> Deny(StatusChangeRequestEntity document)
        {
            var existingMetadataRecord = GetMetadataRecordByUri(document.Uri, true);    
            if (existingMetadataRecord is null) return BadRequest("Document not found for the given approver.");

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
            if (!reset.IsSuccess) return BadRequest($"Reset document failed. Code {reset.Code}");
            var reindex = await _searchService.RunIndexer(String.Empty);
            if (!reindex.IsSuccess) return BadRequest($"Reindex failed. Code {reindex.Code}");

            return Ok();
        }

        [HttpPost]
        [Route("api/documents/justify")]
        public async Task<IActionResult> SubmitJustification(StatusChangeRequestEntity document)
        {
            var existingMetadataRecord = GetMetadataRecordByUri(document.Uri);
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
            if (!reset.IsSuccess) return BadRequest($"Reset document failed. Code {reset.Code}");
            var reindex = await _searchService.RunIndexer(String.Empty);
            if (!reindex.IsSuccess) return BadRequest($"Reindex failed. Code {reindex.Code}");

            return Ok();
        }

        private MetadataRecord? GetMetadataRecordByUri(string uri, bool adminOnly = false)
        {
            var user = User.Identity?.Name;
            if (user is null) return null;
            if (adminOnly && !_authorizationService.Authorize(User)) return null;
            var cosmosDocument = _cosmosService.GetMetadataRecordByAuthorAndUri(user, uri);
            return cosmosDocument;
        }

    }
}
