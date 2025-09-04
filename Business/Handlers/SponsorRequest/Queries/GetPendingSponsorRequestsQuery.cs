using Business.Services.SponsorRequest;
using Core.Utilities.Results;
using Entities.Dtos;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Microsoft.AspNetCore.Http;
using DataAccess.Abstract;

namespace Business.Handlers.SponsorRequest.Queries
{
    public class GetPendingSponsorRequestsQuery : IRequest<IDataResult<List<SponsorRequestDto>>>
    {
        public class GetPendingSponsorRequestsQueryHandler : IRequestHandler<GetPendingSponsorRequestsQuery, IDataResult<List<SponsorRequestDto>>>
        {
            private readonly ISponsorRequestService _sponsorRequestService;
            private readonly IUserRepository _userRepository;
            private readonly IHttpContextAccessor _httpContextAccessor;

            public GetPendingSponsorRequestsQueryHandler(
                ISponsorRequestService sponsorRequestService,
                IUserRepository userRepository,
                IHttpContextAccessor httpContextAccessor)
            {
                _sponsorRequestService = sponsorRequestService;
                _userRepository = userRepository;
                _httpContextAccessor = httpContextAccessor;
            }

            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<List<SponsorRequestDto>>> Handle(GetPendingSponsorRequestsQuery request, CancellationToken cancellationToken)
            {
                // Get current user ID from JWT token (sponsor)
                var userId = _httpContextAccessor.HttpContext?.User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int sponsorId))
                {
                    return new ErrorDataResult<List<SponsorRequestDto>>("User not authenticated");
                }

                var result = await _sponsorRequestService.GetPendingRequestsAsync(sponsorId);
                if (!result.Success)
                {
                    return new ErrorDataResult<List<SponsorRequestDto>>(result.Message);
                }

                // Convert to DTOs with farmer names
                var dtoList = new List<SponsorRequestDto>();
                foreach (var sponsorRequest in result.Data)
                {
                    var farmer = await _userRepository.GetAsync(u => u.UserId == sponsorRequest.FarmerId);
                    dtoList.Add(new SponsorRequestDto
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
                    });
                }

                return new SuccessDataResult<List<SponsorRequestDto>>(dtoList, result.Message);
            }
        }
    }
}