using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Business.Services.AdminAudit;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using MediatR;

namespace Business.Handlers.AdminSponsorship.Commands
{
    /// <summary>
    /// Admin command to refund a sponsorship purchase
    /// </summary>
    public class RefundPurchaseCommand : IRequest<IResult>
    {
        public int PurchaseId { get; set; }
        public string RefundReason { get; set; }

        // Admin context
        public int AdminUserId { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public string RequestPath { get; set; }

        public class RefundPurchaseCommandHandler : IRequestHandler<RefundPurchaseCommand, IResult>
        {
            private readonly ISponsorshipPurchaseRepository _purchaseRepository;
            private readonly ISponsorshipCodeRepository _codeRepository;
            private readonly IAdminAuditService _auditService;

            public RefundPurchaseCommandHandler(
                ISponsorshipPurchaseRepository purchaseRepository,
                ISponsorshipCodeRepository codeRepository,
                IAdminAuditService auditService)
            {
                _purchaseRepository = purchaseRepository;
                _codeRepository = codeRepository;
                _auditService = auditService;
            }

            [SecuredOperation(Priority = 1)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IResult> Handle(RefundPurchaseCommand request, CancellationToken cancellationToken)
            {
                var purchase = await _purchaseRepository.GetAsync(p => p.Id == request.PurchaseId);
                if (purchase == null)
                {
                    return new ErrorResult("Purchase not found");
                }

                if (purchase.PaymentStatus == "Refunded")
                {
                    return new ErrorResult("Purchase is already refunded");
                }

                // Check if any codes have been used
                if (purchase.CodesUsed > 0)
                {
                    return new ErrorResult($"Cannot refund: {purchase.CodesUsed} codes have already been redeemed by farmers");
                }

                var beforeState = new
                {
                    purchase.PaymentStatus,
                    purchase.Status,
                    purchase.CodesGenerated,
                    purchase.CodesUsed
                };

                purchase.PaymentStatus = "Refunded";
                purchase.Status = "Cancelled";
                purchase.UpdatedDate = DateTime.Now;
                purchase.Notes = string.IsNullOrEmpty(purchase.Notes)
                    ? $"[Refunded] {request.RefundReason}"
                    : $"{purchase.Notes}\n[Refunded] {request.RefundReason}";

                // Deactivate all unused codes associated with this purchase
                var codes = _codeRepository.GetList(c => c.SponsorshipPurchaseId == purchase.Id && !c.IsUsed);
                foreach (var code in codes)
                {
                    code.IsActive = false;
                }

                _purchaseRepository.Update(purchase);
                await _purchaseRepository.SaveChangesAsync();

                // Audit log
                await _auditService.LogAsync(
                    action: "RefundPurchase",
                    adminUserId: request.AdminUserId,
                    targetUserId: purchase.SponsorId,
                    entityType: "SponsorshipPurchase",
                    entityId: purchase.Id,
                    isOnBehalfOf: false,
                    ipAddress: request.IpAddress,
                    userAgent: request.UserAgent,
                    requestPath: request.RequestPath,
                    reason: request.RefundReason,
                    beforeState: beforeState,
                    afterState: new
                    {
                        purchase.PaymentStatus,
                        purchase.Status,
                        DeactivatedCodes = codes.Count()
                    }
                );

                return new SuccessResult($"Purchase refunded successfully. {codes.Count()} unused codes have been deactivated.");
            }
        }
    }
}
