using Business.Services.Sponsorship;
using Core.Utilities.Results;
using Entities.Concrete;
using MediatR;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Sponsorship.Commands
{
    public class RedeemSponsorshipCodeCommand : IRequest<IDataResult<UserSubscription>>
    {
        [JsonPropertyName("code")]
        [Required(ErrorMessage = "Sponsorship code is required")]
        public string Code { get; set; }
        
        public int UserId { get; set; }
        public string UserEmail { get; set; } // For logging
        public string UserFullName { get; set; } // For logging

        public class RedeemSponsorshipCodeCommandHandler : IRequestHandler<RedeemSponsorshipCodeCommand, IDataResult<UserSubscription>>
        {
            private readonly ISponsorshipService _sponsorshipService;

            public RedeemSponsorshipCodeCommandHandler(ISponsorshipService sponsorshipService)
            {
                _sponsorshipService = sponsorshipService;
            }

            public async Task<IDataResult<UserSubscription>> Handle(RedeemSponsorshipCodeCommand request, CancellationToken cancellationToken)
            {
                // Log the redemption attempt
                System.Console.WriteLine($"[SponsorshipRedeem] User {request.UserEmail} attempting to redeem code: {request.Code}");

                var result = await _sponsorshipService.RedeemSponsorshipCodeAsync(request.Code, request.UserId);

                if (result.Success)
                {
                    System.Console.WriteLine($"[SponsorshipRedeem] ✅ Code {request.Code} successfully redeemed by user {request.UserEmail}");
                }
                else
                {
                    System.Console.WriteLine($"[SponsorshipRedeem] ❌ Failed to redeem code {request.Code} for user {request.UserEmail}: {result.Message}");
                }

                return result;
            }
        }
    }
}