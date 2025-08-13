using Business.Services.Sponsorship;
using Core.Utilities.Results;
using Entities.Concrete;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Sponsorship.Commands
{
    public class PurchaseBulkSponsorshipCommand : IRequest<IDataResult<SponsorshipPurchase>>
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
        public int ValidityDays { get; set; } = 365;
        public string Notes { get; set; }

        public class PurchaseBulkSponsorshipCommandHandler : IRequestHandler<PurchaseBulkSponsorshipCommand, IDataResult<SponsorshipPurchase>>
        {
            private readonly ISponsorshipService _sponsorshipService;

            public PurchaseBulkSponsorshipCommandHandler(ISponsorshipService sponsorshipService)
            {
                _sponsorshipService = sponsorshipService;
            }

            public async Task<IDataResult<SponsorshipPurchase>> Handle(PurchaseBulkSponsorshipCommand request, CancellationToken cancellationToken)
            {
                try
                {
                    Console.WriteLine($"[PurchaseBulkSponsorship] Starting bulk purchase for SponsorId: {request.SponsorId}, TierId: {request.SubscriptionTierId}, Quantity: {request.Quantity}");
                    
                    var result = await _sponsorshipService.PurchaseBulkSubscriptionsAsync(
                        request.SponsorId,
                        request.SubscriptionTierId,
                        request.Quantity,
                        request.TotalAmount,
                        request.PaymentReference
                    );

                    Console.WriteLine($"[PurchaseBulkSponsorship] Service result: Success={result.Success}, Message={result.Message}");
                    return result;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PurchaseBulkSponsorship] Error in handler: {ex.Message}");
                    Console.WriteLine($"[PurchaseBulkSponsorship] Stack trace: {ex.StackTrace}");
                    return new ErrorDataResult<SponsorshipPurchase>($"Error processing bulk purchase: {ex.Message}");
                }
            }
        }
    }
}