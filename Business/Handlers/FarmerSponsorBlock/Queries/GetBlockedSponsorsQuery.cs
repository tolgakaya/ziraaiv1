using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.FarmerSponsorBlock.Queries
{
    /// <summary>
    /// Gets list of blocked sponsors for a farmer
    /// </summary>
    public class GetBlockedSponsorsQuery : IRequest<IDataResult<List<BlockedSponsorDto>>>
    {
        public int FarmerId { get; set; }

        public class GetBlockedSponsorsQueryHandler : IRequestHandler<GetBlockedSponsorsQuery, IDataResult<List<BlockedSponsorDto>>>
        {
            private readonly IFarmerSponsorBlockRepository _blockRepository;

            public GetBlockedSponsorsQueryHandler(IFarmerSponsorBlockRepository blockRepository)
            {
                _blockRepository = blockRepository;
            }

            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<List<BlockedSponsorDto>>> Handle(GetBlockedSponsorsQuery request, CancellationToken cancellationToken)
            {
                var blocks = await _blockRepository.GetListAsync(b =>
                    b.FarmerId == request.FarmerId && b.IsBlocked);

                var blockedSponsors = blocks.Select(b => new BlockedSponsorDto
                {
                    SponsorId = b.SponsorId,
                    SponsorName = b.Sponsor?.FullName,
                    IsBlocked = b.IsBlocked,
                    IsMuted = b.IsMuted,
                    BlockedDate = b.CreatedDate,
                    Reason = b.Reason
                }).ToList();

                return new SuccessDataResult<List<BlockedSponsorDto>>(blockedSponsors);
            }
        }
    }

    public class BlockedSponsorDto
    {
        public int SponsorId { get; set; }
        public string SponsorName { get; set; }
        public bool IsBlocked { get; set; }
        public bool IsMuted { get; set; }
        public System.DateTime BlockedDate { get; set; }
        public string Reason { get; set; }
    }
}
