using System;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Business.Services.AdminAudit;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using MediatR;

namespace Business.Handlers.AdminSponsorship.Commands
{
    /// <summary>
    /// Admin command to create sponsorship purchase on behalf of a sponsor
    /// Payment can be optional (manual/offline payment scenario)
    /// </summary>
    public class CreatePurchaseOnBehalfOfCommand : IRequest<IDataResult<SponsorshipPurchase>>
    {
        public int SponsorId { get; set; }
        public int SubscriptionTierId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string Currency { get; set; } = "TRY";
        
        // Payment Information (Optional for manual/offline payments)
        public string PaymentMethod { get; set; } // Manual, Offline, BankTransfer, etc.
        public string PaymentReference { get; set; }
        public bool AutoApprove { get; set; } = false; // If true, auto-approve without payment
        
        // Invoice Information
        public string CompanyName { get; set; }
        public string TaxNumber { get; set; }
        public string InvoiceAddress { get; set; }
        
        // Code Generation Settings
        public string CodePrefix { get; set; }
        public int ValidityDays { get; set; } = 365;
        public string Notes { get; set; }

        // Admin context
        public int AdminUserId { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public string RequestPath { get; set; }

        public class CreatePurchaseOnBehalfOfCommandHandler : IRequestHandler<CreatePurchaseOnBehalfOfCommand, IDataResult<SponsorshipPurchase>>
        {
            private readonly ISponsorshipPurchaseRepository _purchaseRepository;
            private readonly ISubscriptionTierRepository _tierRepository;
            private readonly IUserRepository _userRepository;
            private readonly IAdminAuditService _auditService;

            public CreatePurchaseOnBehalfOfCommandHandler(
                ISponsorshipPurchaseRepository purchaseRepository,
                ISubscriptionTierRepository tierRepository,
                IUserRepository userRepository,
                IAdminAuditService auditService)
            {
                _purchaseRepository = purchaseRepository;
                _tierRepository = tierRepository;
                _userRepository = userRepository;
                _auditService = auditService;
            }

            [SecuredOperation(Priority = 1)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<SponsorshipPurchase>> Handle(CreatePurchaseOnBehalfOfCommand request, CancellationToken cancellationToken)
            {
                // Validate sponsor exists
                var sponsor = await _userRepository.GetAsync(u => u.UserId == request.SponsorId);
                if (sponsor == null)
                {
                    return new ErrorDataResult<SponsorshipPurchase>("Sponsor user not found");
                }

                // Validate tier exists
                var tier = await _tierRepository.GetAsync(t => t.Id == request.SubscriptionTierId);
                if (tier == null)
                {
                    return new ErrorDataResult<SponsorshipPurchase>("Subscription tier not found");
                }

                var now = DateTime.Now;
                var totalAmount = request.UnitPrice * request.Quantity;

                var purchase = new SponsorshipPurchase
                {
                    SponsorId = request.SponsorId,
                    SubscriptionTierId = request.SubscriptionTierId,
                    Quantity = request.Quantity,
                    UnitPrice = request.UnitPrice,
                    TotalAmount = totalAmount,
                    Currency = request.Currency,
                    PurchaseDate = now,
                    PaymentMethod = request.PaymentMethod ?? "Manual",
                    PaymentReference = request.PaymentReference ?? $"ADMIN-{now:yyyyMMddHHmmss}",
                    PaymentStatus = request.AutoApprove ? "Completed" : "Pending",
                    PaymentCompletedDate = request.AutoApprove ? now : (DateTime?)null,
                    CompanyName = request.CompanyName,
                    TaxNumber = request.TaxNumber,
                    InvoiceAddress = request.InvoiceAddress,
                    CodePrefix = request.CodePrefix ?? "ADMIN",
                    ValidityDays = request.ValidityDays,
                    CodesGenerated = 0, // Will be generated separately
                    CodesUsed = 0,
                    Status = request.AutoApprove ? "Active" : "Pending",
                    Notes = $"[Created by Admin on behalf of sponsor]\n{request.Notes}",
                    CreatedDate = now,
                    ApprovedByUserId = request.AutoApprove ? request.AdminUserId : (int?)null,
                    ApprovalDate = request.AutoApprove ? now : (DateTime?)null
                };

                _purchaseRepository.Add(purchase);
                await _purchaseRepository.SaveChangesAsync();

                // Audit log
                await _auditService.LogAsync(
                    action: "CreatePurchaseOnBehalfOf",
                    adminUserId: request.AdminUserId,
                    targetUserId: request.SponsorId,
                    entityType: "SponsorshipPurchase",
                    entityId: purchase.Id,
                    isOnBehalfOf: true,
                    ipAddress: request.IpAddress,
                    userAgent: request.UserAgent,
                    requestPath: request.RequestPath,
                    reason: $"Created purchase for sponsor {sponsor.FullName}: {request.Quantity} x {tier.TierName}",
                    afterState: new
                    {
                        purchase.Id,
                        purchase.SponsorId,
                        purchase.Quantity,
                        purchase.TotalAmount,
                        purchase.PaymentStatus,
                        purchase.Status,
                        AutoApproved = request.AutoApprove
                    }
                );

                var message = request.AutoApprove 
                    ? $"Purchase created and auto-approved for {sponsor.FullName}. Total: {totalAmount:C} {request.Currency}"
                    : $"Purchase created for {sponsor.FullName}. Awaiting approval. Total: {totalAmount:C} {request.Currency}";

                return new SuccessDataResult<SponsorshipPurchase>(purchase, message);
            }
        }
    }
}
