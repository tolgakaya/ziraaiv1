using System;
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
    /// Admin command to approve a pending sponsorship purchase
    /// </summary>
    public class ApprovePurchaseCommand : IRequest<IResult>
    {
        public int PurchaseId { get; set; }
        public string Notes { get; set; }

        // Admin context
        public int AdminUserId { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public string RequestPath { get; set; }

        public class ApprovePurchaseCommandHandler : IRequestHandler<ApprovePurchaseCommand, IResult>
        {
            private readonly ISponsorshipPurchaseRepository _purchaseRepository;
            private readonly IAdminAuditService _auditService;

            public ApprovePurchaseCommandHandler(
                ISponsorshipPurchaseRepository purchaseRepository,
                IAdminAuditService auditService)
            {
                _purchaseRepository = purchaseRepository;
                _auditService = auditService;
            }

            [SecuredOperation(Priority = 1)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IResult> Handle(ApprovePurchaseCommand request, CancellationToken cancellationToken)
            {
                var purchase = await _purchaseRepository.GetAsync(p => p.Id == request.PurchaseId);
                if (purchase == null)
                {
                    return new ErrorResult("Purchase not found");
                }

                if (purchase.PaymentStatus == "Completed" && purchase.ApprovedByUserId.HasValue)
                {
                    return new ErrorResult("Purchase is already approved");
                }

                var beforeState = new
                {
                    purchase.PaymentStatus,
                    purchase.ApprovedByUserId,
                    purchase.ApprovalDate,
                    purchase.Status
                };

                purchase.PaymentStatus = "Completed";
                purchase.PaymentCompletedDate = DateTime.Now;
                purchase.ApprovedByUserId = request.AdminUserId;
                purchase.ApprovalDate = DateTime.Now;
                purchase.Status = "Active";
                purchase.UpdatedDate = DateTime.Now;

                if (!string.IsNullOrEmpty(request.Notes))
                {
                    purchase.Notes = string.IsNullOrEmpty(purchase.Notes) 
                        ? request.Notes 
                        : $"{purchase.Notes}\n[Admin Approval] {request.Notes}";
                }

                _purchaseRepository.Update(purchase);
                await _purchaseRepository.SaveChangesAsync();

                // Audit log
                await _auditService.LogAsync(
                    action: "ApprovePurchase",
                    adminUserId: request.AdminUserId,
                    targetUserId: purchase.SponsorId,
                    entityType: "SponsorshipPurchase",
                    entityId: purchase.Id,
                    isOnBehalfOf: false,
                    ipAddress: request.IpAddress,
                    userAgent: request.UserAgent,
                    requestPath: request.RequestPath,
                    reason: $"Approved sponsorship purchase for {purchase.Quantity} subscriptions",
                    beforeState: beforeState,
                    afterState: new
                    {
                        purchase.PaymentStatus,
                        purchase.ApprovedByUserId,
                        purchase.ApprovalDate,
                        purchase.Status
                    }
                );

                return new SuccessResult($"Purchase approved successfully. {purchase.Quantity} codes can now be generated.");
            }
        }
    }
}
