using System;
using System.Collections.Generic;

namespace Entities.DTOs
{
    public class LinkStatisticsDto
    {
        // Code statistics
        public int TotalCodes { get; set; }
        public int UsedCodes { get; set; }
        public int UnusedCodes { get; set; }
        public int ExpiredCodes { get; set; }
        public int ActiveCodes { get; set; }

        // Link statistics
        public int TotalLinksGenerated { get; set; }
        public int TotalLinksSent { get; set; }
        public int TotalLinksClicked { get; set; }
        public int TotalClickCount { get; set; }

        // Delivery statistics by channel
        public int SmsDelivered { get; set; }
        public int WhatsAppDelivered { get; set; }
        public int EmailDelivered { get; set; }

        // Performance metrics
        public double AverageClicksPerLink { get; set; }
        public decimal ConversionRate { get; set; } // Percentage of links that resulted in redemption
        public decimal ClickThroughRate { get; set; } // Percentage of sent links that were clicked

        // Time-based statistics
        public List<DailyStatistic> DailyStatistics { get; set; } = new();
        public List<ChannelPerformance> ChannelPerformance { get; set; } = new();

        // Date range
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class DailyStatistic
    {
        public DateTime Date { get; set; }
        public int CodesCreated { get; set; }
        public int LinksSent { get; set; }
        public int LinksClicked { get; set; }
        public int CodesRedeemed { get; set; }
    }

    public class ChannelPerformance
    {
        public string Channel { get; set; } // SMS, WhatsApp, Email
        public int TotalSent { get; set; }
        public int Delivered { get; set; }
        public int Clicked { get; set; }
        public int Redeemed { get; set; }
        public decimal DeliveryRate { get; set; }
        public decimal ClickRate { get; set; }
        public decimal ConversionRate { get; set; }
    }
}