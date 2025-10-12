using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Sponsorship.Commands
{
    public class CreateSponsorshipCodeCommand : IRequest<IDataResult<SponsorshipCode>>
    {
        public int SponsorId { get; set; }
        public string FarmerName { get; set; }
        public string FarmerPhone { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
        public DateTime? ExpiryDate { get; set; }

        public class CreateSponsorshipCodeCommandHandler : IRequestHandler<CreateSponsorshipCodeCommand, IDataResult<SponsorshipCode>>
        {
            private readonly ISponsorshipCodeRepository _sponsorshipCodeRepository;

            public CreateSponsorshipCodeCommandHandler(ISponsorshipCodeRepository sponsorshipCodeRepository)
            {
                _sponsorshipCodeRepository = sponsorshipCodeRepository;
            }

            public async Task<IDataResult<SponsorshipCode>> Handle(CreateSponsorshipCodeCommand request, CancellationToken cancellationToken)
            {
                try
                {
                    // Generate unique code
                    var code = GenerateUniqueCode();
                    
                    var sponsorshipCode = new SponsorshipCode
                    {
                        Code = code,
                        SponsorId = request.SponsorId,
                        RecipientName = request.FarmerName,
                        RecipientPhone = request.FarmerPhone,
                        Notes = request.Description,
                        ExpiryDate = request.ExpiryDate ?? DateTime.Now.AddDays(30),
                        IsActive = true,
                        IsUsed = false,
                        CreatedDate = DateTime.Now,
                        SubscriptionTierId = 2, // Default to Medium tier for individual sponsorships
                        SponsorshipPurchaseId = 1, // Will be fixed when we implement proper purchase linking
                        
                        // Initialize link tracking fields
                        LinkClickCount = 0,
                        LinkDelivered = false
                    };

                    _sponsorshipCodeRepository.Add(sponsorshipCode);
                    await _sponsorshipCodeRepository.SaveChangesAsync();

                    return new SuccessDataResult<SponsorshipCode>(sponsorshipCode, "Sponsorship code created successfully");
                }
                catch (Exception ex)
                {
                    return new ErrorDataResult<SponsorshipCode>($"Error creating sponsorship code: {ex.Message}");
                }
            }

            private string GenerateUniqueCode()
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd");
                var random = new Random().Next(100000, 999999);
                return $"SPONSOR-{timestamp}-{random}";
            }
        }
    }
}