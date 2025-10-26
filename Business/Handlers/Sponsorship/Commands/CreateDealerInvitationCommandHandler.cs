using Core.Entities.Concrete;
using Core.Utilities.Results;
using Core.Utilities.Security.Hashing;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Dtos;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Sponsorship.Commands
{
    /// <summary>
    /// Handler for creating dealer invitations (Invite or AutoCreate types)
    /// Authorization: Sponsor role only
    /// </summary>
    public class CreateDealerInvitationCommandHandler : IRequestHandler<CreateDealerInvitationCommand, IDataResult<DealerInvitationResponseDto>>
    {
        private readonly IDealerInvitationRepository _dealerInvitationRepository;
        private readonly ISponsorshipCodeRepository _sponsorshipCodeRepository;
        private readonly IUserRepository _userRepository;
        private readonly IGroupRepository _groupRepository;
        private readonly IUserGroupRepository _userGroupRepository;

        public CreateDealerInvitationCommandHandler(
            IDealerInvitationRepository dealerInvitationRepository,
            ISponsorshipCodeRepository sponsorshipCodeRepository,
            IUserRepository userRepository,
            IGroupRepository groupRepository,
            IUserGroupRepository userGroupRepository)
        {
            _dealerInvitationRepository = dealerInvitationRepository;
            _sponsorshipCodeRepository = sponsorshipCodeRepository;
            _userRepository = userRepository;
            _groupRepository = groupRepository;
            _userGroupRepository = userGroupRepository;
        }

        public async Task<IDataResult<DealerInvitationResponseDto>> Handle(CreateDealerInvitationCommand request, CancellationToken cancellationToken)
        {
            // 1. Validate request
            if (request.InvitationType == "Invite" && string.IsNullOrWhiteSpace(request.Email))
            {
                return new ErrorDataResult<DealerInvitationResponseDto>("Email is required for Invite type.");
            }

            // 2. Check if sponsor has enough unused codes
            var purchaseCodes = await _sponsorshipCodeRepository.GetByPurchaseIdAsync(request.PurchaseId);
            var availableCodesCount = purchaseCodes.Count(c => 
                c.SponsorId == request.SponsorId 
                && !c.IsUsed 
                && c.IsActive 
                && c.ExpiryDate > DateTime.Now
                && c.DealerId == null);

            if (availableCodesCount < request.CodeCount)
            {
                return new ErrorDataResult<DealerInvitationResponseDto>(
                    $"Not enough available codes. Requested: {request.CodeCount}, Available: {availableCodesCount}");
            }

            // 3. Create invitation token
            var invitationToken = Guid.NewGuid().ToString("N");

            // 4. Create dealer invitation entity
            var invitation = new DealerInvitation
            {
                SponsorId = request.SponsorId,
                Email = request.Email,
                Phone = request.Phone,
                DealerName = request.DealerName,
                Status = "Pending",
                InvitationType = request.InvitationType,
                InvitationToken = invitationToken,
                PurchaseId = request.PurchaseId,
                CodeCount = request.CodeCount,
                CreatedDate = DateTime.Now,
                ExpiryDate = DateTime.Now.AddDays(7) // 7 days expiry
            };

            // 5. Handle AutoCreate type - create dealer account immediately
            if (request.InvitationType == "AutoCreate")
            {
                var autoPassword = GenerateRandomPassword();
                
                // Create dealer user account
                var newDealer = new User
                {
                    Email = request.Email,
                    FullName = request.DealerName,
                    Status = true
                };

                // Hash password
                HashingHelper.CreatePasswordHash(autoPassword, out byte[] passwordHash, out byte[] passwordSalt);
                newDealer.PasswordHash = passwordHash;
                newDealer.PasswordSalt = passwordSalt;

                // Save new dealer
                var createdDealer = _userRepository.Add(newDealer);
                await _userRepository.SaveChangesAsync();

                // Assign Sponsor role to new dealer (using UserGroup entity)
                var sponsorGroup = await _groupRepository.GetAsync(g => g.GroupName == "Sponsor");
                if (sponsorGroup != null)
                {
                    var userGroup = new UserGroup
                    {
                        UserId = createdDealer.UserId,
                        GroupId = sponsorGroup.Id
                    };
                    _userGroupRepository.Add(userGroup);
                    await _userGroupRepository.SaveChangesAsync();
                }

                // Update invitation with created dealer info
                invitation.CreatedDealerId = createdDealer.UserId;
                invitation.AutoCreatedPassword = autoPassword; // Store plain password for one-time retrieval
                invitation.Status = "Accepted"; // Auto-accepted
                invitation.AcceptedDate = DateTime.Now;

                // Transfer codes immediately
                await TransferCodesToDealer(request.PurchaseId, request.SponsorId, createdDealer.UserId, request.CodeCount);
            }

            // 6. Save invitation
            _dealerInvitationRepository.Add(invitation);
            await _dealerInvitationRepository.SaveChangesAsync();

            // 7. Build response
            var response = new DealerInvitationResponseDto
            {
                InvitationId = invitation.Id,
                InvitationToken = invitationToken,
                InvitationLink = request.InvitationType == "Invite" 
                    ? $"https://ziraai.com/dealer-invitation?token={invitationToken}" 
                    : null,
                Email = request.Email,
                Phone = request.Phone,
                DealerName = request.DealerName,
                CodeCount = request.CodeCount,
                Status = invitation.Status,
                InvitationType = request.InvitationType,
                AutoCreatedPassword = invitation.AutoCreatedPassword,
                CreatedDealerId = invitation.CreatedDealerId,
                CreatedAt = invitation.CreatedDate
            };

            var message = request.InvitationType == "AutoCreate"
                ? $"Dealer account created successfully. Login: {request.Email}, Password: {invitation.AutoCreatedPassword}"
                : $"Invitation sent to {request.Email}";

            return new SuccessDataResult<DealerInvitationResponseDto>(response, message);
        }

        private async Task TransferCodesToDealer(int purchaseId, int sponsorId, int dealerId, int codeCount)
        {
            var codes = await _sponsorshipCodeRepository.GetByPurchaseIdAsync(purchaseId);
            var codesToTransfer = codes
                .Where(c => c.SponsorId == sponsorId 
                         && !c.IsUsed 
                         && c.IsActive 
                         && c.ExpiryDate > DateTime.Now
                         && c.DealerId == null)
                .Take(codeCount)
                .ToList();

            var transferTime = DateTime.Now;
            foreach (var code in codesToTransfer)
            {
                code.DealerId = dealerId;
                code.TransferredAt = transferTime;
                code.TransferredByUserId = sponsorId;
                _sponsorshipCodeRepository.Update(code);
            }
            await _sponsorshipCodeRepository.SaveChangesAsync();
        }

        private string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 12).Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
