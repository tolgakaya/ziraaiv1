using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Sponsorship.Queries
{
    /// <summary>
    /// Get authenticated user's pending dealer invitations
    /// </summary>
    public class GetMyPendingInvitationsQuery : IRequest<IDataResult<PendingInvitationsResponseDto>>
    {
        public string UserEmail { get; set; }
        public string UserPhone { get; set; }

        public class GetMyPendingInvitationsQueryHandler : IRequestHandler<GetMyPendingInvitationsQuery, IDataResult<PendingInvitationsResponseDto>>
        {
            private readonly IDealerInvitationRepository _invitationRepository;
            private readonly ISponsorProfileRepository _sponsorProfileRepository;
            private readonly ILogger<GetMyPendingInvitationsQueryHandler> _logger;

            public GetMyPendingInvitationsQueryHandler(
                IDealerInvitationRepository invitationRepository,
                ISponsorProfileRepository sponsorProfileRepository,
                ILogger<GetMyPendingInvitationsQueryHandler> logger)
            {
                _invitationRepository = invitationRepository;
                _sponsorProfileRepository = sponsorProfileRepository;
                _logger = logger;
            }

            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<PendingInvitationsResponseDto>> Handle(
                GetMyPendingInvitationsQuery request,
                CancellationToken cancellationToken)
            {
                try
                {
                    _logger.LogInformation("🔍 Fetching pending invitations for Email: {Email}, Phone: {Phone}",
                        request.UserEmail ?? "null", request.UserPhone ?? "null");

                    // Query pending invitations by email OR phone
                    var invitationsQuery = _invitationRepository.Query()
                        .Where(di => di.Status == "Pending" && di.ExpiryDate > DateTime.Now);

                    // Filter by email OR phone
                    if (!string.IsNullOrEmpty(request.UserEmail) && !string.IsNullOrEmpty(request.UserPhone))
                    {
                        // User has both - match either
                        invitationsQuery = invitationsQuery.Where(di =>
                            di.Email == request.UserEmail || di.Phone == request.UserPhone);
                    }
                    else if (!string.IsNullOrEmpty(request.UserEmail))
                    {
                        // Email only
                        invitationsQuery = invitationsQuery.Where(di => di.Email == request.UserEmail);
                    }
                    else if (!string.IsNullOrEmpty(request.UserPhone))
                    {
                        // Phone only
                        invitationsQuery = invitationsQuery.Where(di => di.Phone == request.UserPhone);
                    }
                    else
                    {
                        _logger.LogWarning("❌ No email or phone provided in request");
                        return new ErrorDataResult<PendingInvitationsResponseDto>(
                            "Email veya telefon bilgisi gerekli");
                    }

                    var invitations = await invitationsQuery
                        .OrderBy(di => di.ExpiryDate) // Expiring soon first
                        .ToListAsync(cancellationToken);

                    _logger.LogInformation("📋 Found {Count} pending invitations", invitations.Count);

                    // Map to DTOs with sponsor info
                    var invitationDtos = new System.Collections.Generic.List<DealerInvitationSummaryDto>();

                    foreach (var invitation in invitations)
                    {
                        var sponsor = await _sponsorProfileRepository.GetAsync(s => s.SponsorId == invitation.SponsorId);

                        invitationDtos.Add(new DealerInvitationSummaryDto
                        {
                            InvitationId = invitation.Id,
                            Token = invitation.InvitationToken,
                            SponsorCompanyName = sponsor?.CompanyName ?? "Unknown Sponsor",
                            CodeCount = invitation.CodeCount,
                            PackageTier = invitation.PackageTier ?? "Unknown",
                            ExpiresAt = invitation.ExpiryDate,
                            RemainingDays = (invitation.ExpiryDate - DateTime.Now).Days,
                            Status = invitation.Status,
                            DealerEmail = invitation.Email,
                            DealerPhone = invitation.Phone,
                            CreatedAt = invitation.CreatedDate
                        });
                    }

                    var response = new PendingInvitationsResponseDto
                    {
                        Invitations = invitationDtos,
                        TotalCount = invitationDtos.Count
                    };

                    _logger.LogInformation("✅ Successfully retrieved {Count} pending invitations", response.TotalCount);

                    return new SuccessDataResult<PendingInvitationsResponseDto>(
                        response,
                        $"Found {response.TotalCount} pending invitation(s)");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error retrieving pending invitations");
                    return new ErrorDataResult<PendingInvitationsResponseDto>(
                        "Bekleyen davetiyeler alınırken hata oluştu");
                }
            }
        }
    }
}
