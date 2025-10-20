using Microsoft.AspNetCore.Http;

namespace Entities.Dtos
{
    /// <summary>
    /// DTO for sending voice messages with file upload
    /// </summary>
    public class SendVoiceMessageDto
    {
        public int ToUserId { get; set; }
        public int PlantAnalysisId { get; set; }
        public int Duration { get; set; }
        public string Waveform { get; set; }
        public IFormFile VoiceFile { get; set; }
    }
}
