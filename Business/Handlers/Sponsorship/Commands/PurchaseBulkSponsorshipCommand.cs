using Business.Services.Sponsorship;
using Core.CrossCuttingConcerns.Caching;
using Core.Utilities.Results;
using Entities.Concrete;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Sponsorship.Commands
{
    public class PurchaseBulkSponsorshipCommand : IRequest<IDataResult<Entities.Dtos.SponsorshipPurchaseResponseDto>>
    {
        public int SponsorId { get; set; }
        public int SubscriptionTierId { get; set; }
        public int Quantity { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentReference { get; set; }
        public string CompanyName { get; set; }
        public string InvoiceAddress { get; set; }
        public string TaxNumber { get; set; }
        public string CodePrefix { get; set; } = "AGRI";
        public int ValidityDays { get; set; } = 30;
        public string Notes { get; set; }

        public class PurchaseBulkSponsorshipCommandHandler : IRequestHandler<PurchaseBulkSponsorshipCommand, IDataResult<Entities.Dtos.SponsorshipPurchaseResponseDto>>
        {
            private readonly ISponsorshipService _sponsorshipService;
            private readonly ICacheManager _cacheManager;
            private readonly Business.Services.AdminAnalytics.IAdminStatisticsCacheService _adminCacheService;

            public PurchaseBulkSponsorshipCommandHandler(
                ISponsorshipService sponsorshipService,
                ICacheManager cacheManager,
                Business.Services.AdminAnalytics.IAdminStatisticsCacheService adminCacheService)
            {
                _sponsorshipService = sponsorshipService;
                _cacheManager = cacheManager;
                _adminCacheService = adminCacheService;
            }

            public async Task<IDataResult<Entities.Dtos.SponsorshipPurchaseResponseDto>> Handle(PurchaseBulkSponsorshipCommand request, CancellationToken cancellationToken)
            {
                try
                {
                    Console.WriteLine($"[PurchaseBulkSponsorship] Starting bulk purchase for SponsorId: {request.SponsorId}, TierId: {request.SubscriptionTierId}, Quantity: {request.Quantity}");
                    Console.WriteLine($"[PurchaseBulkSponsorship] Invoice Info - Company: {request.CompanyName}, Tax: {request.TaxNumber}");

                    var result = await _sponsorshipService.PurchaseBulkSubscriptionsAsync(
                        request.SponsorId,
                        request.SubscriptionTierId,
                        request.Quantity,
                        request.TotalAmount,
                        request.PaymentMethod,
                        request.PaymentReference,
                        request.CompanyName,
                        request.InvoiceAddress,
                        request.TaxNumber
                    );

                    // Invalidate sponsor dashboard cache and admin statistics cache after successful purchase
                    if (result.Success)
                    {
                        var cacheKey = $"SponsorDashboard:{request.SponsorId}";
                        _cacheManager.Remove(cacheKey);
                        Console.WriteLine($"[DashboardCache] 🗑️ Invalidated cache for sponsor {request.SponsorId} after purchase");

                        // Invalidate admin statistics cache (sponsorship data changed)
                        await _adminCacheService.InvalidateAllStatisticsAsync();
                        Console.WriteLine($"[AdminStatsCache] 🗑️ Invalidated admin statistics cache after sponsorship purchase");
                    }

                    Console.WriteLine($"[PurchaseBulkSponsorship] Service result: Success={result.Success}, Message={result.Message}");
                    return result;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PurchaseBulkSponsorship] Error in handler: {ex.Message}");
                    Console.WriteLine($"[PurchaseBulkSponsorship] Stack trace: {ex.StackTrace}");
                    return new ErrorDataResult<Entities.Dtos.SponsorshipPurchaseResponseDto>($"Error processing bulk purchase: {ex.Message}");
                }
            }
        }
    }
}