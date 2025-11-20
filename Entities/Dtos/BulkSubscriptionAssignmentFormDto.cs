using Microsoft.AspNetCore.Http;

namespace Entities.Dtos
{
    /// <summary>
    /// Form model for bulk subscription assignment file upload
    /// Used to handle multipart/form-data with IFormFile and other parameters
    /// </summary>
    public class BulkSubscriptionAssignmentFormDto
    {
        public IFormFile ExcelFile { get; set; }
        public int? DefaultTierId { get; set; }
        public int? DefaultDurationDays { get; set; }
        public bool SendNotification { get; set; } = true;
        public string NotificationMethod { get; set; } = "Email";
        public bool AutoActivate { get; set; } = true;
    }
}
