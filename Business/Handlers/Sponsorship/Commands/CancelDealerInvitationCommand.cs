using Business.Services.Notification;
using Core.Aspects.Autofac.Caching;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Sponsorship.Commands
{
    /// <summary>
    /// Command to cancel a pending dealer invitation
    /// </summary>
    public class CancelDealerInvitationCommand : IRequest<IResult>
    {
        public int InvitationId { get; set; }
        public int SponsorId { get; set; } // For authorization check

        public class CancelDealerInvitationCommandHandler : IRequestHandler<CancelDealerInvitationCommand, IResult>
        {
            private readonly IDealerInvitationRepository _invitationRepository;
            private readonly ISponsorshipCodeRepository _codeRepository;
            private readonly ILogger<CancelDealerInvitationCommandHandler> _logger;

            public CancelDealerInvitationCommandHandler(
                IDealerInvitationRepository invitationRepository,
                ISponsorshipCodeRepository codeRepository,
                ILogger<CancelDealerInvitationCommandHandler> logger)
            {
                _invitationRepository = invitationRepository;
                _codeRepository = codeRepository;
                _logger = logger;
            }

            [CacheRemoveAspect("Get")]
            [LogAspect(typeof(FileLogger))]
            public async Task<IResult> Handle(CancelDealerInvitationCommand request, CancellationToken cancellationToken)
            {
                try
                {
                    _logger.LogInformation("🚫 Sponsor {SponsorId} attempting to cancel invitation {InvitationId}",
                        request.SponsorId, request.InvitationId);

                    // 1. Get invitation
                    var invitation = await _invitationRepository.GetAsync(i => i.Id == request.InvitationId);
                    
                    if (invitation == null)
                    {
                        _logger.LogWarning("❌ Invitation {InvitationId} not found", request.InvitationId);
                        return new ErrorResult("Davetiye bulunamadı");
                    }

                    // 2. Authorization check - only the sponsor who created can cancel
                    if (invitation.SponsorId != request.SponsorId)
                    {
                        _logger.LogWarning("⚠️ Sponsor {SponsorId} attempted to cancel invitation {InvitationId} owned by {OwnerId}",
                            request.SponsorId, request.InvitationId, invitation.SponsorId);
                        return new ErrorResult("Bu davetiyeyi iptal etme yetkiniz yok");
                    }

                    // 3. Check if invitation can be cancelled (only Pending invitations)
                    if (invitation.Status != "Pending")
                    {
                        _logger.LogWarning("❌ Cannot cancel invitation {InvitationId} with status {Status}",
                            request.InvitationId, invitation.Status);
                        return new ErrorResult($"Sadece bekleyen davetiyeler iptal edilebilir. Mevcut durum: {invitation.Status}");
                    }

                    // 4. Check if invitation is already expired
                    if (invitation.ExpiryDate < DateTime.Now)
                    {
                        _logger.LogInformation("⏰ Invitation {InvitationId} already expired", request.InvitationId);
                        return new ErrorResult("Davetiye zaten süresi dolmuş");
                    }

                    // 5. Release reserved codes
                    var reservedCodes = await _codeRepository.GetListAsync(c => 
                        c.ReservedForInvitationId == request.InvitationId);

                    var releasedCount = 0;
                    foreach (var code in reservedCodes)
                    {
                        code.ReservedForInvitationId = null;
                        code.ReservedAt = null;
                        _codeRepository.Update(code);
                        releasedCount++;
                    }

                    if (releasedCount > 0)
                    {
                        await _codeRepository.SaveChangesAsync();
                        _logger.LogInformation("✅ Released {Count} reserved codes from invitation {InvitationId}",
                            releasedCount, request.InvitationId);
                    }

                    // 6. Update invitation status
                    invitation.Status = "Cancelled";
                    invitation.CancelledDate = DateTime.Now;
                    invitation.CancelledByUserId = request.SponsorId;
                    
                    _invitationRepository.Update(invitation);
                    await _invitationRepository.SaveChangesAsync();

                    _logger.LogInformation("✅ Successfully cancelled invitation {InvitationId}. Released {CodeCount} codes",
                        request.InvitationId, releasedCount);

                    return new SuccessResult($"Davetiye iptal edildi. {releasedCount} kod serbest bırakıldı");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error cancelling invitation {InvitationId}", request.InvitationId);
                    return new ErrorResult("Davetiye iptal edilirken hata oluştu");
                }
            }
        }
    }
}
