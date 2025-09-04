using Core.Entities;
using Core.Entities.Concrete;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Concrete
{
    [Table("DeepLinks")]
    public class DeepLink : IEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string LinkId { get; set; } // Unique identifier for the link

        [Required]
        [StringLength(50)]
        public string Type { get; set; } // redemption, analysis, dashboard, profile

        [StringLength(200)]
        public string PrimaryParameter { get; set; } // code, analysisId, userId, etc.

        [StringLength(500)]
        public string AdditionalParameters { get; set; } // Serialized key-value pairs

        [Required]
        [StringLength(500)]
        public string DeepLinkUrl { get; set; } // ziraai://redemption?code=XXX

        [StringLength(500)]
        public string UniversalLinkUrl { get; set; } // https://ziraai.com/redeem/XXX

        [StringLength(500)]
        public string WebFallbackUrl { get; set; } // https://web.ziraai.com/redeem/XXX

        [StringLength(200)]
        public string ShortUrl { get; set; } // https://ziraai.com/s/abc123

        [StringLength(5000)]
        public string QrCodeUrl { get; set; } // Base64 QR code image

        [StringLength(50)]
        public string CampaignSource { get; set; } // sms, whatsapp, email, manual

        [StringLength(50)]
        public string SponsorId { get; set; } // Associated sponsor if applicable

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime ExpiryDate { get; set; }
        public bool IsActive { get; set; } = true;

        // Analytics counters
        public int TotalClicks { get; set; } = 0;
        public int MobileAppOpens { get; set; } = 0;
        public int WebFallbackOpens { get; set; } = 0;
        public int UniqueDevices { get; set; } = 0;
        public DateTime? LastClickDate { get; set; }
    }

    [Table("DeepLinkClickRecords")]
    public class DeepLinkClickRecord : IEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string LinkId { get; set; } // References DeepLink.LinkId

        [StringLength(500)]
        public string UserAgent { get; set; }

        [StringLength(45)]
        public string IpAddress { get; set; } // IPv4/IPv6

        [StringLength(20)]
        public string Platform { get; set; } // iOS, Android, Web, Mobile

        [StringLength(100)]
        public string DeviceId { get; set; } // For unique device tracking

        [StringLength(500)]
        public string Referrer { get; set; }

        public DateTime ClickDate { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string Country { get; set; }

        [StringLength(100)]
        public string City { get; set; }

        [StringLength(50)]
        public string Source { get; set; } // sms, whatsapp, email, direct

        // Action tracking
        public bool DidOpenApp { get; set; } = false; // Updated via webhook/callback
        public bool DidCompleteAction { get; set; } = false; // e.g., redemption completed
        public DateTime? ActionCompletedDate { get; set; }
        public string ActionResult { get; set; } // success, failed, partial

        // Navigation properties
        public virtual DeepLink DeepLink { get; set; }
    }
}