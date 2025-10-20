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
        /// Validate a single attachment based on feature flags and ANALYSIS tier
        /// NOTE: Validation is based on the analysis tier, not user tier
        /// </summary>
        Task<IResult> ValidateAttachmentAsync(IFormFile file, int plantAnalysisId, string attachmentType);

        /// <summary>
        /// Validate multiple attachments based on ANALYSIS tier
        /// NOTE: Validation is based on the analysis tier, not user tier
        /// </summary>
        Task<IDataResult<List<string>>> ValidateAttachmentsAsync(List<IFormFile> files, int plantAnalysisId);

        /// <summary>
        /// Get allowed MIME types for analysis tier
        /// </summary>
        Task<List<string>> GetAllowedMimeTypesAsync(int plantAnalysisId, string attachmentType);

        /// <summary>
        /// Get max file size for analysis tier
        /// </summary>
        Task<long> GetMaxFileSizeAsync(int plantAnalysisId, string attachmentType);
    }

    public class AttachmentValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
        public string AttachmentType { get; set; } // ImageAttachments, VideoAttachments, FileAttachments
    }
}
