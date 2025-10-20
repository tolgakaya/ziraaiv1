using Core.Utilities.Results;
using IResult = Core.Utilities.Results.IResult;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Business.Services.Messaging
{
    public interface IAttachmentValidationService
    {
        /// <summary>
        /// Validate a single attachment based on feature flags and user tier
        /// </summary>
        Task<IResult> ValidateAttachmentAsync(IFormFile file, int userId, string attachmentType);

        /// <summary>
        /// Validate multiple attachments
        /// </summary>
        Task<IDataResult<List<string>>> ValidateAttachmentsAsync(List<IFormFile> files, int userId);

        /// <summary>
        /// Get allowed MIME types for user's tier
        /// </summary>
        Task<List<string>> GetAllowedMimeTypesAsync(int userId, string attachmentType);

        /// <summary>
        /// Get max file size for user's tier
        /// </summary>
        Task<long> GetMaxFileSizeAsync(int userId, string attachmentType);
    }

    public class AttachmentValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
        public string AttachmentType { get; set; } // ImageAttachments, VideoAttachments, FileAttachments
    }
}
