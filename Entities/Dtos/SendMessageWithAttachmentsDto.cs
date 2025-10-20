using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace Entities.Dtos
{
    /// <summary>
    /// DTO for sending messages with file attachments
    /// </summary>
    public class SendMessageWithAttachmentsDto
    {
        public int ToUserId { get; set; }
        public int PlantAnalysisId { get; set; }
        public string Message { get; set; }
        public string MessageType { get; set; }
        public List<IFormFile> Attachments { get; set; }
    }
}
