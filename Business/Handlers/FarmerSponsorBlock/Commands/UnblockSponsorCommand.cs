using Business.Constants;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.FarmerSponsorBlock.Commands
{
    /// <summary>
    /// Farmer unblocks a sponsor
    /// </summary>
    public class UnblockSponsorCommand : IRequest<IResult>
    {
        public int FarmerId { get; set; }
        public int SponsorId { get; set; }

        public class UnblockSponsorCommandHandler : IRequestHandler<UnblockSponsorCommand, IResult>
        {
            private readonly IFarmerSponsorBlockRepository _blockRepository;

            public UnblockSponsorCommandHandler(IFarmerSponsorBlockRepository blockRepository)
            {
                _blockRepository = blockRepository;
            }

            [LogAspect(typeof(FileLogger))]
            public async Task<IResult> Handle(UnblockSponsorCommand request, CancellationToken cancellationToken)
            {
                var existingBlock = await _blockRepository.GetBlockRecordAsync(request.FarmerId, request.SponsorId);

                if (existingBlock != null)
                {
                    existingBlock.IsBlocked = false;
                    _blockRepository.Update(existingBlock);
                    await _blockRepository.SaveChangesAsync();
                }

                return new SuccessResult("Sponsor has been unblocked successfully");
            }
        }
    }
}
