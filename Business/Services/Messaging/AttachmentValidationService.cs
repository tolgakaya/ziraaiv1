using Core.Utilities.Results;
using IResult = Core.Utilities.Results.IResult;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Business.Services.Messaging
{
    public class AttachmentValidationService : IAttachmentValidationService
    {
        private readonly IMessagingFeatureService _featureService;

        // MIME type categorization
        private static readonly Dictionary<string, string> MimeTypeToFeature = new()
        {
            // Images
            { "image/jpeg", "ImageAttachments" },
            { "image/jpg", "ImageAttachments" },
            { "image/png", "ImageAttachments" },
            { "image/webp", "ImageAttachments" },
            { "image/heic", "ImageAttachments" },
            { "image/gif", "ImageAttachments" },

            // Videos
            { "video/mp4", "VideoAttachments" },
            { "video/mov", "VideoAttachments" },
            { "video/avi", "VideoAttachments" },
            { "video/quicktime", "VideoAttachments" },

            // Documents
            { "application/pdf", "FileAttachments" },
            { "application/msword", "FileAttachments" },
            { "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "FileAttachments" },
            { "application/vnd.ms-excel", "FileAttachments" },
            { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "FileAttachments" },
            { "text/plain", "FileAttachments" },
            { "text/csv", "FileAttachments" }
        };

        public AttachmentValidationService(IMessagingFeatureService featureService)
        {
            _featureService = featureService;
        }

        public async Task<IResult> ValidateAttachmentAsync(IFormFile file, int userId, string attachmentType)
        {
            if (file == null || file.Length == 0)
                return new ErrorResult("File is empty or null");

            // Determine attachment type from MIME type if not provided
            var mimeType = file.ContentType.ToLowerInvariant();
            if (string.IsNullOrEmpty(attachmentType))
            {
                if (!MimeTypeToFeature.TryGetValue(mimeType, out attachmentType))
                {
                    return new ErrorResult($"Unsupported file type: {mimeType}");
                }
            }

            // Check feature availability and constraints
            var validationResult = await _featureService.ValidateFeatureAccessAsync(
                attachmentType,
                userId,
                file.Length);

            if (!validationResult.Success)
                return validationResult;

            // Additional MIME type validation
            var allowedTypes = await GetAllowedMimeTypesAsync(userId, attachmentType);
            if (!allowedTypes.Contains(mimeType))
            {
                return new ErrorResult($"File type {mimeType} not allowed for {attachmentType}");
            }

            return new SuccessResult("Attachment validated successfully");
        }

        public async Task<IDataResult<List<string>>> ValidateAttachmentsAsync(List<IFormFile> files, int userId)
        {
            if (files == null || files.Count == 0)
                return new ErrorDataResult<List<string>>("No files provided");

            var errors = new List<string>();
            var validatedTypes = new List<string>();

            foreach (var file in files)
            {
                var mimeType = file.ContentType.ToLowerInvariant();
                if (!MimeTypeToFeature.TryGetValue(mimeType, out var attachmentType))
                {
                    errors.Add($"{file.FileName}: Unsupported file type {mimeType}");
                    continue;
                }

                var result = await ValidateAttachmentAsync(file, userId, attachmentType);
                if (!result.Success)
                {
                    errors.Add($"{file.FileName}: {result.Message}");
                }
                else
                {
                    validatedTypes.Add(attachmentType);
                }
            }

            if (errors.Any())
            {
                return new ErrorDataResult<List<string>>(
                    validatedTypes,
                    $"Validation failed for {errors.Count} file(s): {string.Join("; ", errors)}");
            }

            return new SuccessDataResult<List<string>>(
                validatedTypes,
                $"{files.Count} attachment(s) validated successfully");
        }

        public async Task<List<string>> GetAllowedMimeTypesAsync(int userId, string attachmentType)
        {
            var feature = await _featureService.GetFeatureAsync(attachmentType);

            if (!feature.Success || feature.Data == null)
                return new List<string>();

            if (string.IsNullOrEmpty(feature.Data.AllowedMimeTypes))
                return new List<string>();

            return feature.Data.AllowedMimeTypes
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim().ToLowerInvariant())
                .ToList();
        }

        public async Task<long> GetMaxFileSizeAsync(int userId, string attachmentType)
        {
            var feature = await _featureService.GetFeatureAsync(attachmentType);

            if (!feature.Success || feature.Data == null)
                return 0;

            return feature.Data.MaxFileSize ?? 0;
        }
    }
}
