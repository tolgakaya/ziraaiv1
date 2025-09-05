using System.Collections.Generic;

namespace Business.Services.Notification.Models
{
    /// <summary>
    /// WhatsApp message structure for API communication
    /// </summary>
    public class WhatsAppMessageDto
    {
        /// <summary>
        /// Recipient phone number in international format (e.g., 905551234567)
        /// </summary>
        public string To { get; set; }

        /// <summary>
        /// Message type (text, template, image, etc.)
        /// </summary>
        public string Type { get; set; } = "template";

        /// <summary>
        /// Template information for template messages
        /// </summary>
        public WhatsAppTemplateDto Template { get; set; }

        /// <summary>
        /// Text content for simple text messages
        /// </summary>
        public WhatsAppTextDto Text { get; set; }
    }

    /// <summary>
    /// WhatsApp text message content
    /// </summary>
    public class WhatsAppTextDto
    {
        /// <summary>
        /// Message body text (max 4096 characters)
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// Preview URL setting
        /// </summary>
        public bool PreviewUrl { get; set; } = true;
    }

    /// <summary>
    /// WhatsApp template message structure
    /// </summary>
    public class WhatsAppTemplateDto
    {
        /// <summary>
        /// Template name registered with WhatsApp
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Template language code (e.g., tr, en_US)
        /// </summary>
        public WhatsAppLanguageDto Language { get; set; } = new() { Code = "tr" };

        /// <summary>
        /// Template components (header, body, buttons)
        /// </summary>
        public List<WhatsAppComponentDto> Components { get; set; } = new();
    }

    /// <summary>
    /// WhatsApp template language specification
    /// </summary>
    public class WhatsAppLanguageDto
    {
        /// <summary>
        /// Language code (tr for Turkish)
        /// </summary>
        public string Code { get; set; }
    }

    /// <summary>
    /// WhatsApp template component (header, body, footer, buttons)
    /// </summary>
    public class WhatsAppComponentDto
    {
        /// <summary>
        /// Component type (header, body, footer, button)
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Component parameters for dynamic values
        /// </summary>
        public List<WhatsAppParameterDto> Parameters { get; set; } = new();

        /// <summary>
        /// Sub-type for buttons
        /// </summary>
        public string SubType { get; set; }

        /// <summary>
        /// Button index for button components
        /// </summary>
        public int? Index { get; set; }
    }

    /// <summary>
    /// WhatsApp template parameter for dynamic content
    /// </summary>
    public class WhatsAppParameterDto
    {
        /// <summary>
        /// Parameter type (text, currency, date_time, image, document)
        /// </summary>
        public string Type { get; set; } = "text";

        /// <summary>
        /// Parameter text value
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Parameter image for media parameters
        /// </summary>
        public WhatsAppImageDto Image { get; set; }
    }

    /// <summary>
    /// WhatsApp image parameter
    /// </summary>
    public class WhatsAppImageDto
    {
        /// <summary>
        /// Image URL or media ID
        /// </summary>
        public string Link { get; set; }
    }
}