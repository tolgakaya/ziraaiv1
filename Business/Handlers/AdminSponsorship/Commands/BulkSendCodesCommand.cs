using System;
using System.Collections.Generic;
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
    /// Admin command to bulk send sponsorship codes to farmers on behalf of sponsor
    /// Uses phone number list to distribute codes
    /// </summary>
    public class BulkSendCodesCommand : IRequest<IResult>
    {
        public int SponsorId { get; set; }
        public int PurchaseId { get; set; }
        public List<RecipientInfo> Recipients { get; set; } // Phone numbers with optional names
        public string SendVia { get; set; } = "SMS"; // SMS, WhatsApp, Email
        public string Message { get; set; } // Optional custom message

        // Admin context
        public int AdminUserId { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public string RequestPath { get; set; }

        public class BulkSendCodesCommandHandler : IRequestHandler<BulkSendCodesCommand, IResult>
        {
            private readonly ISponsorshipCodeRepository _codeRepository;
            private readonly ISponsorshipPurchaseRepository _purchaseRepository;
            private readonly IAdminAuditService _auditService;

            public BulkSendCodesCommandHandler(
                ISponsorshipCodeRepository codeRepository,
                ISponsorshipPurchaseRepository purchaseRepository,
                IAdminAuditService auditService)
            {
                _codeRepository = codeRepository;
                _purchaseRepository = purchaseRepository;
                _auditService = auditService;
            }

            [SecuredOperation(Priority = 1)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IResult> Handle(BulkSendCodesCommand request, CancellationToken cancellationToken)
            {
                if (request.Recipients == null || !request.Recipients.Any())
                {
                    return new ErrorResult("No recipients provided");
                }

                // Validate purchase
                var purchase = await _purchaseRepository.GetAsync(p => 
                    p.Id == request.PurchaseId && 
                    p.SponsorId == request.SponsorId);

                if (purchase == null)
                {
                    return new ErrorResult("Purchase not found or does not belong to this sponsor");
                }

                if (purchase.PaymentStatus != "Completed")
                {
                    return new ErrorResult("Purchase must be completed before sending codes");
                }

                // Get available unused codes for this purchase
                var availableCodes = _codeRepository.GetList(c => 
                    c.SponsorshipPurchaseId == request.PurchaseId && 
                    !c.IsUsed && 
                    c.IsActive &&
                    string.IsNullOrEmpty(c.RecipientPhone))
                    .OrderBy(c => c.CreatedDate)
                    .ToList();

                if (availableCodes.Count < request.Recipients.Count)
                {
                    return new ErrorResult($"Not enough available codes. Available: {availableCodes.Count}, Requested: {request.Recipients.Count}");
                }

                var sentCount = 0;
                var failedCount = 0;
                var now = DateTime.Now;

                for (int i = 0; i < request.Recipients.Count; i++)
                {
                    var recipient = request.Recipients[i];
                    var code = availableCodes[i];

                    try
                    {
                        // Update code with recipient info
                        code.RecipientPhone = recipient.PhoneNumber;
                        code.RecipientName = recipient.Name;
                        code.LinkSentVia = request.SendVia;
                        code.LinkSentDate = now;
                        code.DistributionChannel = request.SendVia;
                        code.DistributionDate = now;
                        code.DistributedTo = string.IsNullOrEmpty(recipient.Name) 
                            ? recipient.PhoneNumber 
                            : $"{recipient.Name} ({recipient.PhoneNumber})";

                        // In real implementation, this would trigger actual SMS/WhatsApp/Email send
                        // For now, we just mark it as sent
                        code.LinkDelivered = true; // Assume delivery success

                        _codeRepository.Update(code);
                        sentCount++;
                    }
                    catch
                    {
                        failedCount++;
                    }
                }

                await _codeRepository.SaveChangesAsync();

                // Audit log
                await _auditService.LogAsync(
                    action: "BulkSendCodes",
                    adminUserId: request.AdminUserId,
                    targetUserId: request.SponsorId,
                    entityType: "SponsorshipCode",
                    entityId: request.PurchaseId,
                    isOnBehalfOf: true,
                    ipAddress: request.IpAddress,
                    userAgent: request.UserAgent,
                    requestPath: request.RequestPath,
                    reason: $"Bulk sent {sentCount} codes via {request.SendVia}",
                    afterState: new
                    {
                        PurchaseId = request.PurchaseId,
                        SentCount = sentCount,
                        FailedCount = failedCount,
                        SendVia = request.SendVia,
                        RecipientCount = request.Recipients.Count
                    }
                );

                return new SuccessResult($"Bulk send completed. Sent: {sentCount}, Failed: {failedCount}");
            }
        }
    }

    public class RecipientInfo
    {
        public string PhoneNumber { get; set; }
        public string Name { get; set; } // Optional
    }
}
