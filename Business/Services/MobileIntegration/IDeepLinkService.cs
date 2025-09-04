using Core.Utilities.Results;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Business.Services.MobileIntegration
{
    public interface IDeepLinkService
    {
        Task<IDataResult<DeepLinkInfo>> GenerateDeepLinkAsync(DeepLinkRequest request);
        Task<IDataResult<DeepLinkAnalytics>> GetDeepLinkAnalyticsAsync(string linkId);
        Task<IResult> TrackDeepLinkClickAsync(string linkId, DeepLinkClickInfo clickInfo);
        Task<IDataResult<UniversalLinkConfig>> GetUniversalLinkConfigAsync();
    }

    public class DeepLinkRequest
    {
        public string Type { get; set; } // "redemption", "analysis", "dashboard"
        public string PrimaryParameter { get; set; } // Code, analysisId, etc.
        public Dictionary<string, string> AdditionalParameters { get; set; }
        public string FallbackUrl { get; set; }
        public string CampaignSource { get; set; } // "sms", "whatsapp", "email"
        public string SponsorId { get; set; }
        public bool GenerateQrCode { get; set; } = false;
        public string CustomScheme { get; set; } = "ziraai";
    }

    public class DeepLinkInfo
    {
        public string LinkId { get; set; }
        public string DeepLinkUrl { get; set; } // ziraai://redeem?code=XXX
        public string UniversalLinkUrl { get; set; } // https://ziraai.com/redeem/XXX
        public string WebFallbackUrl { get; set; } // https://web.ziraai.com/redeem/XXX
        public string QrCodeUrl { get; set; } // Base64 or URL to QR code image
        public string ShortUrl { get; set; } // bit.ly style short URL
        public DateTime CreatedDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class DeepLinkAnalytics
    {
        public string LinkId { get; set; }
        public int TotalClicks { get; set; }
        public int MobileAppOpens { get; set; }
        public int WebFallbackOpens { get; set; }
        public int UniqueDevices { get; set; }
        public Dictionary<string, int> PlatformBreakdown { get; set; } // iOS, Android, Web
        public Dictionary<string, int> SourceBreakdown { get; set; } // SMS, WhatsApp, etc.
        public List<DeepLinkClick> RecentClicks { get; set; }
        public double ConversionRate { get; set; } // Click to redemption rate
    }

    public class DeepLinkClickInfo
    {
        public string LinkId { get; set; }
        public string UserAgent { get; set; }
        public string IpAddress { get; set; }
        public string Platform { get; set; } // iOS, Android, Web
        public string DeviceId { get; set; } // For unique device counting
        public string Referrer { get; set; }
        public DateTime ClickDate { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
    }

    public class DeepLinkClick
    {
        public DateTime ClickDate { get; set; }
        public string Platform { get; set; }
        public string Country { get; set; }
        public string Source { get; set; }
        public bool DidOpenApp { get; set; }
        public bool DidCompleteAction { get; set; } // e.g., redemption completed
    }

    public class UniversalLinkConfig
    {
        public string Domain { get; set; } // ziraai.com
        public string AppId { get; set; } // com.ziraai.app
        public Dictionary<string, string> PathMappings { get; set; } // /redeem/* -> redemption
        public string AppleAppStoreUrl { get; set; }
        public string GooglePlayStoreUrl { get; set; }
        public string WebFallbackDomain { get; set; }
        public bool IsConfigured { get; set; }
    }

    public class QrCodeOptions
    {
        public int Size { get; set; } = 256; // 256x256 pixels
        public string ForegroundColor { get; set; } = "#000000";
        public string BackgroundColor { get; set; } = "#FFFFFF";
        public string Logo { get; set; } // Base64 logo to embed
        public string Format { get; set; } = "PNG"; // PNG, JPG, SVG
        public int ErrorCorrectionLevel { get; set; } = 2; // L=1, M=2, Q=3, H=4
    }
}