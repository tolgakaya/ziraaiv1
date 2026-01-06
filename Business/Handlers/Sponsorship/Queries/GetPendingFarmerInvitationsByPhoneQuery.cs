using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Core.Aspects.Autofac.Logging;
using Core.Aspects.Autofac.Performance;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Helpers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Business.Handlers.Sponsorship.Queries
{
    /// <summary>
    /// Query to get pending farmer invitations by phone number
    /// Returns all pending (not yet accepted) invitations for the farmer's phone
    /// </summary>
    public class GetPendingFarmerInvitationsByPhoneQuery : IRequest<IDataResult<List<FarmerInvitationListDto>>>
    {
        public string Phone { get; set; }
    }

    public class GetPendingFarmerInvitationsByPhoneQueryHandler : IRequestHandler<GetPendingFarmerInvitationsByPhoneQuery, IDataResult<List<FarmerInvitationListDto>>>
    {
        private readonly IFarmerInvitationRepository _farmerInvitationRepository;
        private readonly ILogger<GetPendingFarmerInvitationsByPhoneQueryHandler> _logger;

        public GetPendingFarmerInvitationsByPhoneQueryHandler(
            IFarmerInvitationRepository farmerInvitationRepository,
            ILogger<GetPendingFarmerInvitationsByPhoneQueryHandler> logger)
        {
            _farmerInvitationRepository = farmerInvitationRepository;
            _logger = logger;
        }

        [SecuredOperation(Priority = 1)]
        [PerformanceAspect(5)]
        [LogAspect(typeof(FileLogger))]
        public async Task<IDataResult<List<FarmerInvitationListDto>>> Handle(GetPendingFarmerInvitationsByPhoneQuery request, CancellationToken cancellationToken)
        {
            // Normalize phone number using centralized helper (always returns +90XXXXXXXXXX format)
            var normalizedPhone = PhoneNumberHelper.NormalizePhoneNumber(request.Phone);

            _logger.LogInformation("[FARMER_INV] Original: {Original}, Normalized: {Normalized}",
                request.Phone, normalizedPhone);

            // Get all pending invitations for this phone number
            // All invitations should now use the same normalized format (+90XXXXXXXXXX)
            var invitations = await _farmerInvitationRepository.GetListAsync(i =>
                i.Phone == normalizedPhone &&
                i.Status == "Pending");

            _logger.LogInformation("[FARMER_INV] Found {Count} pending invitations", invitations?.Count() ?? 0);

            if (invitations != null && invitations.Any())
            {
                foreach (var inv in invitations)
                {
                    _logger.LogInformation("[FARMER_INV] Invitation - Id: {Id}, Phone: {Phone}, Status: {Status}",
                        inv.Id, inv.Phone, inv.Status);
                }
            }

            if (invitations == null || !invitations.Any())
            {
                return new SuccessDataResult<List<FarmerInvitationListDto>>(
                    new List<FarmerInvitationListDto>(),
                    "No pending invitations found"
                );
            }

            // Map to DTOs
            var dtoList = invitations.Select(i => new FarmerInvitationListDto
            {
                Id = i.Id,
                Phone = i.Phone,
                FarmerName = i.FarmerName,
                Email = i.Email,
                InvitationToken = i.InvitationToken,
                Status = i.Status,
                CodeCount = i.CodeCount,
                PackageTier = i.PackageTier,
                AcceptedByUserId = i.AcceptedByUserId,
                AcceptedDate = i.AcceptedDate,
                CreatedDate = i.CreatedDate,
                ExpiryDate = i.ExpiryDate,
                LinkDelivered = i.LinkDelivered,
                LinkSentDate = i.LinkSentDate
            }).OrderByDescending(x => x.CreatedDate).ToList();

            return new SuccessDataResult<List<FarmerInvitationListDto>>(
                dtoList,
                $"{dtoList.Count} pending invitation(s) found"
            );
        }

        // Removed - now using PhoneNumberHelper.NormalizePhoneNumber() for consistency
        // All phone numbers in the system now use the +90XXXXXXXXXX format
    }
}
