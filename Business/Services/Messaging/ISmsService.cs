using Core.Utilities.Results;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Business.Services.Messaging
{
    public interface ISmsService
    {
        Task<IResult> SendSmsAsync(string phoneNumber, string message);
        Task<IResult> SendBulkSmsAsync(BulkSmsRequest request);
        Task<IDataResult<SmsDeliveryStatus>> GetDeliveryStatusAsync(string messageId);
        Task<IDataResult<SmsSenderInfo>> GetSenderInfoAsync();
    }

    public class BulkSmsRequest
    {
        public SmsRecipient[] Recipients { get; set; }
        public string Message { get; set; }
        public string SenderId { get; set; } = "ZiraAI";
        public bool ScheduledSend { get; set; } = false;
        public DateTime? ScheduledDate { get; set; }
        public int MaxRetryAttempts { get; set; } = 3;
    }

    public class SmsRecipient
    {
        public string PhoneNumber { get; set; }
        public string Name { get; set; }
        public string MessageId { get; set; }
        public string PersonalizedMessage { get; set; }
    }

    public class SmsDeliveryStatus
    {
        public string MessageId { get; set; }
        public string PhoneNumber { get; set; }
        public string Status { get; set; } // Sent, Delivered, Failed, Pending
        public DateTime SentDate { get; set; }
        public DateTime? DeliveredDate { get; set; }
        public string ErrorMessage { get; set; }
        public decimal Cost { get; set; }
        public string Provider { get; set; }
    }

    public class SmsSenderInfo
    {
        public string SenderId { get; set; }
        public decimal Balance { get; set; }
        public string Currency { get; set; }
        public int MonthlyQuota { get; set; }
        public int UsedQuota { get; set; }
        public string Provider { get; set; }
        public bool IsActive { get; set; }
    }
}