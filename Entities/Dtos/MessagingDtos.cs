using System;
using System.Collections.Generic;

namespace Entities.Dtos
{
    public class BulkSmsRequest
    {
        public List<SmsRecipient> Recipients { get; set; }
        public string Message { get; set; }
        public string ScheduledTime { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
        public bool UseTemplate { get; set; }
        public string TemplateName { get; set; }
    }

    public class SmsRecipient
    {
        public string PhoneNumber { get; set; }
        public string Name { get; set; }
        public Dictionary<string, object> PersonalizationData { get; set; }
    }

    public class SmsDeliveryStatus
    {
        public string MessageId { get; set; }
        public string Status { get; set; } // "Pending", "Sent", "Delivered", "Failed"
        public string PhoneNumber { get; set; }
        public DateTime SentAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public string ErrorMessage { get; set; }
        public string Provider { get; set; }
        public decimal Cost { get; set; }
    }

    public class BulkWhatsAppRequest
    {
        public List<WhatsAppRecipient> Recipients { get; set; }
        public string Message { get; set; }
        public string TemplateName { get; set; }
        public Dictionary<string, object> TemplateParameters { get; set; }
        public string ScheduledTime { get; set; }
        public List<string> MediaUrls { get; set; }
    }

    public class WhatsAppRecipient
    {
        public string PhoneNumber { get; set; }
        public string Name { get; set; }
        public Dictionary<string, object> PersonalizationData { get; set; }
    }

    public class WhatsAppDeliveryStatus
    {
        public string MessageId { get; set; }
        public string Status { get; set; } // "Pending", "Sent", "Delivered", "Read", "Failed"
        public string PhoneNumber { get; set; }
        public DateTime SentAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public string ErrorMessage { get; set; }
        public decimal Cost { get; set; }
    }

    public class MessageTemplate
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; } // "SMS", "WhatsApp", "Email"
        public string Subject { get; set; }
        public string Content { get; set; }
        public List<string> Variables { get; set; }
        public string Language { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class MessageStatistics
    {
        public int TotalSent { get; set; }
        public int TotalDelivered { get; set; }
        public int TotalFailed { get; set; }
        public int TotalPending { get; set; }
        public decimal DeliveryRate { get; set; }
        public decimal TotalCost { get; set; }
        public Dictionary<string, int> StatusBreakdown { get; set; }
        public Dictionary<string, int> ProviderBreakdown { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
    }

    public class PhoneNumberValidation
    {
        public string PhoneNumber { get; set; }
        public bool IsValid { get; set; }
        public string FormattedNumber { get; set; }
        public string Country { get; set; }
        public string CountryCode { get; set; }
        public string Carrier { get; set; }
        public string NumberType { get; set; } // "Mobile", "Landline", "VoIP", "Unknown"
        public List<string> Issues { get; set; }
    }

    public class ProviderConfiguration
    {
        public string ProviderId { get; set; }
        public string ProviderName { get; set; }
        public string Type { get; set; } // "SMS", "WhatsApp"
        public Dictionary<string, string> Settings { get; set; }
        public bool IsActive { get; set; }
        public int Priority { get; set; }
        public List<string> SupportedCountries { get; set; }
        public decimal CostPerMessage { get; set; }
    }

    public class SendResult
    {
        public bool Success { get; set; }
        public string MessageId { get; set; }
        public string Status { get; set; }
        public string ErrorMessage { get; set; }
        public decimal Cost { get; set; }
        public string Provider { get; set; }
        public DateTime SentAt { get; set; }
    }

    // WhatsApp template-related DTOs
    public class WhatsAppTemplateRequest
    {
        public string RecipientPhone { get; set; }
        public string TemplateName { get; set; }
        public string LanguageCode { get; set; }
        public List<TemplateComponent> Components { get; set; }
    }

    public class TemplateComponent
    {
        public string Type { get; set; } // "header", "body", "button"
        public string SubType { get; set; }
        public List<ComponentParameter> Parameters { get; set; }
    }

    public class ComponentParameter
    {
        public string Type { get; set; } // "text", "currency", "date_time"
        public string Text { get; set; }
        public string Currency { get; set; }
        public decimal? Amount { get; set; }
        public DateTime? DateTime { get; set; }
    }
}