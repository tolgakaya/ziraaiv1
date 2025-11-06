using Microsoft.AspNetCore.Http;

namespace Entities.Dtos
{
    /// <summary>
    /// Form model for bulk code distribution file upload
    /// Used to handle multipart/form-data with IFormFile and other parameters
    /// </summary>
    public class BulkCodeDistributionFormDto
    {
        public IFormFile ExcelFile { get; set; }
        public bool SendSms { get; set; } = false;
    }
}
