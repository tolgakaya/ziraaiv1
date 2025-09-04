using Business.Services.SponsorRequest;
using Core.Utilities.Results;
using Entities.Dtos;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using DataAccess.Abstract;

namespace Business.Handlers.SponsorRequest.Queries
{
    public class ProcessDeeplinkQuery : IRequest<IDataResult<SponsorRequestDto>>
    {
        public string HashedToken { get; set; }

        public class ProcessDeeplinkQueryHandler : IRequestHandler<ProcessDeeplinkQuery, IDataResult<SponsorRequestDto>>
        {
            private readonly ISponsorRequestService _sponsorRequestService;
            private readonly IUserRepository _userRepository;

            public ProcessDeeplinkQueryHandler(
                ISponsorRequestService sponsorRequestService,
                IUserRepository userRepository)
            {
                _sponsorRequestService = sponsorRequestService;
                _userRepository = userRepository;
            }

            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<SponsorRequestDto>> Handle(ProcessDeeplinkQuery request, CancellationToken cancellationToken)
            {
                var result = await _sponsorRequestService.ProcessDeeplinkAsync(request.HashedToken);
                if (!result.Success)
                {
                    return new ErrorDataResult<SponsorRequestDto>(result.Message);
                }

                var sponsorRequest = result.Data;
                
                // Get farmer details
                var farmer = await _userRepository.GetAsync(u => u.UserId == sponsorRequest.FarmerId);
                var sponsor = await _userRepository.GetAsync(u => u.UserId == sponsorRequest.SponsorId);

                var dto = new SponsorRequestDto
                {
                    Id = sponsorRequest.Id,
                    FarmerId = sponsorRequest.FarmerId,
                    SponsorId = sponsorRequest.SponsorId,
                    FarmerPhone = sponsorRequest.FarmerPhone,
                    SponsorPhone = sponsorRequest.SponsorPhone,
                    FarmerName = farmer?.FullName,
                    RequestMessage = sponsorRequest.RequestMessage,
                    RequestDate = sponsorRequest.RequestDate,
                    Status = sponsorRequest.Status,
                    ApprovalDate = sponsorRequest.ApprovalDate,
                    ApprovedSubscriptionTierId = sponsorRequest.ApprovedSubscriptionTierId,
                    ApprovalNotes = sponsorRequest.ApprovalNotes,
                    GeneratedSponsorshipCode = sponsorRequest.GeneratedSponsorshipCode
                };

                return new SuccessDataResult<SponsorRequestDto>(dto, "Deeplink processed successfully");
            }
        }
    }
}