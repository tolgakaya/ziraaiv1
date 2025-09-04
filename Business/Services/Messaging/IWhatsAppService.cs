using Core.Utilities.Results;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Business.Services.Messaging
{
    public interface IWhatsAppService
    {
        Task<IResult> SendMessageAsync(string phoneNumber, string message);
        Task<IResult> SendTemplateMessageAsync(string phoneNumber, string templateName, object templateParameters);
        Task<IResult> SendBulkMessageAsync(BulkWhatsAppRequest request);
        Task<IDataResult<WhatsAppDeliveryStatus>> GetDeliveryStatusAsync(string messageId);
        Task<IDataResult<WhatsAppAccountInfo>> GetAccountInfoAsync();
    }

    public class BulkWhatsAppRequest
    {
        public WhatsAppRecipient[] Recipients { get; set; }
        public string TemplateName { get; set; }
        public string Message { get; set; }
        public bool UseTemplate { get; set; } = false;
        public int MaxRetryAttempts { get; set; } = 3;
    }

    public class WhatsAppRecipient
    {
        public string PhoneNumber { get; set; }
        public string Name { get; set; }
        public string MessageId { get; set; }
        public object TemplateParameters { get; set; }
        public string PersonalizedMessage { get; set; }
    }

    public class WhatsAppDeliveryStatus
    {
        public string MessageId { get; set; }
        public string PhoneNumber { get; set; }
        public string Status { get; set; } // sent, delivered, read, failed
        public DateTime SentDate { get; set; }
        public DateTime? DeliveredDate { get; set; }
        public DateTime? ReadDate { get; set; }
        public string ErrorMessage { get; set; }
        public string Provider { get; set; } = "WhatsApp Business";
    }

    public class WhatsAppAccountInfo
    {
        public string BusinessPhoneNumber { get; set; }
        public string BusinessName { get; set; }
        public string AccountStatus { get; set; }
        public int MonthlyMessageQuota { get; set; }
        public int UsedMessages { get; set; }
        public string Currency { get; set; }
        public bool IsVerified { get; set; }
    }

    public class WhatsAppTemplate
    {
        public string Name { get; set; }
        public string Language { get; set; }
        public string Status { get; set; } // APPROVED, PENDING, REJECTED
        public string Category { get; set; } // MARKETING, UTILITY, AUTHENTICATION
        public WhatsAppTemplateComponent[] Components { get; set; }
    }

    public class WhatsAppTemplateComponent
    {
        public string Type { get; set; } // HEADER, BODY, FOOTER, BUTTONS
        public string Text { get; set; }
        public WhatsAppTemplateParameter[] Parameters { get; set; }
    }

    public class WhatsAppTemplateParameter
    {
        public string Type { get; set; } // text, currency, date_time
        public string Value { get; set; }
    }
}