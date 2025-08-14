using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Core.Aspects.Autofac.Caching;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.DTOs;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Business.Handlers.Sponsorship.Queries
{
    public class GetLinkStatisticsQuery : IRequest<IDataResult<LinkStatisticsDto>>
    {
        public int SponsorId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public class GetLinkStatisticsQueryHandler : IRequestHandler<GetLinkStatisticsQuery, IDataResult<LinkStatisticsDto>>
        {
            private readonly ISponsorshipCodeRepository _codeRepository;
            private readonly ILogger<GetLinkStatisticsQueryHandler> _logger;

            public GetLinkStatisticsQueryHandler(
                ISponsorshipCodeRepository codeRepository,
                ILogger<GetLinkStatisticsQueryHandler> logger)
            {
                _codeRepository = codeRepository;
                _logger = logger;
            }

            [SecuredOperation(Priority = 1)]
            [CacheAspect(10)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<LinkStatisticsDto>> Handle(GetLinkStatisticsQuery request, CancellationToken cancellationToken)
            {
                try
                {
                    var startDate = request.StartDate ?? DateTime.Now.AddDays(-30);
                    var endDate = request.EndDate ?? DateTime.Now;

                    // Get all codes for the sponsor
                    var codes = await _codeRepository.GetListAsync(c => 
                        c.SponsorId == request.SponsorId &&
                        c.CreatedDate >= startDate &&
                        c.CreatedDate <= endDate);

                    // Calculate statistics
                    var statistics = new LinkStatisticsDto
                    {
                        TotalCodes = codes.Count(),
                        UsedCodes = codes.Count(c => c.IsUsed),
                        UnusedCodes = codes.Count(c => !c.IsUsed),
                        ExpiredCodes = codes.Count(c => c.ExpiryDate < DateTime.Now),
                        ActiveCodes = codes.Count(c => c.IsActive && !c.IsUsed && c.ExpiryDate >= DateTime.Now),
                        
                        // Link statistics
                        TotalLinksGenerated = codes.Count(c => !string.IsNullOrEmpty(c.RedemptionLink)),
                        TotalLinksSent = codes.Count(c => c.LinkSentDate.HasValue),
                        TotalLinksClicked = codes.Count(c => c.LinkClickDate.HasValue),
                        TotalClickCount = codes.Sum(c => c.LinkClickCount),
                        
                        // Delivery statistics
                        SmsDelivered = codes.Count(c => c.LinkSentVia == "SMS" && c.LinkDelivered),
                        WhatsAppDelivered = codes.Count(c => c.LinkSentVia == "WhatsApp" && c.LinkDelivered),
                        EmailDelivered = codes.Count(c => c.LinkSentVia == "Email" && c.LinkDelivered),
                        
                        // Performance metrics
                        AverageClicksPerLink = codes.Where(c => c.LinkClickCount > 0).Any() 
                            ? codes.Where(c => c.LinkClickCount > 0).Average(c => c.LinkClickCount) 
                            : 0,
                        ConversionRate = codes.Count(c => !string.IsNullOrEmpty(c.RedemptionLink)) > 0
                            ? (decimal)codes.Count(c => c.IsUsed) / codes.Count(c => !string.IsNullOrEmpty(c.RedemptionLink)) * 100
                            : 0,
                        ClickThroughRate = codes.Count(c => c.LinkSentDate.HasValue) > 0
                            ? (decimal)codes.Count(c => c.LinkClickDate.HasValue) / codes.Count(c => c.LinkSentDate.HasValue) * 100
                            : 0,
                        
                        // Time-based statistics
                        DailyStatistics = CalculateDailyStatistics(codes.ToList(), startDate, endDate),
                        ChannelPerformance = CalculateChannelPerformance(codes.ToList()),
                        
                        StartDate = startDate,
                        EndDate = endDate
                    };

                    _logger.LogInformation("Link statistics retrieved for sponsor {SponsorId}", request.SponsorId);

                    return new SuccessDataResult<LinkStatisticsDto>(statistics);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting link statistics for sponsor {SponsorId}", request.SponsorId);
                    return new ErrorDataResult<LinkStatisticsDto>("İstatistikler alınırken hata oluştu");
                }
            }

            private List<DailyStatistic> CalculateDailyStatistics(
                List<Entities.Concrete.SponsorshipCode> codes, 
                DateTime startDate, 
                DateTime endDate)
            {
                var dailyStats = new List<DailyStatistic>();
                var currentDate = startDate.Date;

                while (currentDate <= endDate.Date)
                {
                    var dayCodes = codes.Where(c => 
                        c.CreatedDate.Date == currentDate ||
                        c.LinkSentDate?.Date == currentDate ||
                        c.LinkClickDate?.Date == currentDate ||
                        c.UsedDate?.Date == currentDate).ToList();

                    if (dayCodes.Any())
                    {
                        dailyStats.Add(new DailyStatistic
                        {
                            Date = currentDate,
                            CodesCreated = codes.Count(c => c.CreatedDate.Date == currentDate),
                            LinksSent = codes.Count(c => c.LinkSentDate?.Date == currentDate),
                            LinksClicked = codes.Count(c => c.LinkClickDate?.Date == currentDate),
                            CodesRedeemed = codes.Count(c => c.UsedDate?.Date == currentDate)
                        });
                    }

                    currentDate = currentDate.AddDays(1);
                }

                return dailyStats;
            }

            private List<ChannelPerformance> CalculateChannelPerformance(List<Entities.Concrete.SponsorshipCode> codes)
            {
                var channels = codes
                    .Where(c => !string.IsNullOrEmpty(c.LinkSentVia))
                    .GroupBy(c => c.LinkSentVia)
                    .Select(g => new ChannelPerformance
                    {
                        Channel = g.Key,
                        TotalSent = g.Count(),
                        Delivered = g.Count(c => c.LinkDelivered),
                        Clicked = g.Count(c => c.LinkClickDate.HasValue),
                        Redeemed = g.Count(c => c.IsUsed),
                        DeliveryRate = g.Count() > 0 ? (decimal)g.Count(c => c.LinkDelivered) / g.Count() * 100 : 0,
                        ClickRate = g.Count() > 0 ? (decimal)g.Count(c => c.LinkClickDate.HasValue) / g.Count() * 100 : 0,
                        ConversionRate = g.Count() > 0 ? (decimal)g.Count(c => c.IsUsed) / g.Count() * 100 : 0
                    })
                    .ToList();

                return channels;
            }
        }
    }
}