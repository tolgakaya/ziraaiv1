using Business.Constants;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.FarmerSponsorBlock.Commands
{
    /// <summary>
    /// Farmer blocks a sponsor from sending messages
    /// </summary>
    public class BlockSponsorCommand : IRequest<IResult>
    {
        public int FarmerId { get; set; }
        public int SponsorId { get; set; }
        public string Reason { get; set; }

        public class BlockSponsorCommandHandler : IRequestHandler<BlockSponsorCommand, IResult>
        {
            private readonly IFarmerSponsorBlockRepository _blockRepository;

            public BlockSponsorCommandHandler(IFarmerSponsorBlockRepository blockRepository)
            {
                _blockRepository = blockRepository;
            }

            [LogAspect(typeof(FileLogger))]
            public async Task<IResult> Handle(BlockSponsorCommand request, CancellationToken cancellationToken)
            {
                // Check if block already exists
                var existingBlock = await _blockRepository.GetBlockRecordAsync(request.FarmerId, request.SponsorId);

                if (existingBlock != null)
                {
                    // Update existing block
                    existingBlock.IsBlocked = true;
                    existingBlock.Reason = request.Reason;
                    _blockRepository.Update(existingBlock);
                }
                else
                {
                    // Create new block
                    var newBlock = new Entities.Concrete.FarmerSponsorBlock
                    {
                        FarmerId = request.FarmerId,
                        SponsorId = request.SponsorId,
                        IsBlocked = true,
                        IsMuted = false,
                        CreatedDate = DateTime.Now,
                        Reason = request.Reason
                    };
                    _blockRepository.Add(newBlock);
                }

                await _blockRepository.SaveChangesAsync();
                return new SuccessResult("Sponsor has been blocked successfully");
            }
        }
    }
}
