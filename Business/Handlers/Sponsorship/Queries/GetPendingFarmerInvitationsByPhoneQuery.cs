using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Core.Aspects.Autofac.Logging;
using Core.Aspects.Autofac.Performance;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
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
            // Normalize phone number (handle Turkish format)
            var normalizedPhone = NormalizePhoneNumber(request.Phone);

            // Generate alternative format for matching (both +90 and 0 prefixes)
            var alternativePhone = GetAlternativePhoneFormat(normalizedPhone);

            _logger.LogInformation("[FARMER_INV] Original: {Original}, Normalized: {Normalized}, Alternative: {Alternative}",
                request.Phone, normalizedPhone, alternativePhone);

            // Get all pending invitations for this phone number (check both formats)
            var invitations = await _farmerInvitationRepository.GetListAsync(i =>
                (i.Phone == normalizedPhone || i.Phone == alternativePhone) &&
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

        /// <summary>
        /// Normalize phone number to handle Turkish format (+90 vs 0 prefix)
        /// </summary>
        private string NormalizePhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return phone;

            phone = phone.Trim().Replace(" ", "").Replace("-", "");

            // Convert +90 to 0 prefix (Turkish mobile format)
            if (phone.StartsWith("+90"))
            {
                phone = "0" + phone.Substring(3);
            }
            // Remove leading 90 if present (ensure consistent format)
            else if (phone.StartsWith("90") && phone.Length == 12)
            {
                phone = "0" + phone.Substring(2);
            }

            return phone;
        }

        /// <summary>
        /// Get alternative phone format for matching (handles both +90 and 0 prefix)
        /// </summary>
        private string GetAlternativePhoneFormat(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return phone;

            // If phone starts with 0, return +90 version
            if (phone.StartsWith("0") && phone.Length == 11)
            {
                return "+90" + phone.Substring(1);
            }
            // If phone starts with +90, return 0 version
            else if (phone.StartsWith("+90") && phone.Length == 13)
            {
                return "0" + phone.Substring(3);
            }

            return phone;
        }
    }
}
