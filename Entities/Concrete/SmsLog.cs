using Core.Entities;
using System;

namespace Entities.Concrete
{
    /// <summary>
    /// SMS logging entity for debugging and monitoring
    /// Logs SMS content for dealer invites, code distribution, and referrals
    /// Controlled by configuration flag: SmsLogging:Enabled
    /// </summary>
    public class SmsLog : IEntity
    {
        public int Id { get; set; }
        
        /// <summary>
        /// Action type: DealerInvite, CodeDistribute, Referral
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// User ID of the sender (sponsor/dealer/referrer)
        /// </summary>
        public int? SenderUserId { get; set; }

        /// <summary>
        /// SMS content and metadata as JSON
        /// Includes: phone, message, timestamp, etc.
        /// </summary>
        public string Content { get; set; }
        
        /// <summary>
        /// When the SMS was logged
        /// </summary>
        public DateTime CreatedDate { get; set; }
    }
}
