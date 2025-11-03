using Business.BusinessAspects;
using Business.Services.Sponsorship;
using Core.Utilities.Results;
using Entities.Dtos;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Sponsorship.Commands
{
    public class BulkDealerInvitationCommandHandler
        : IRequestHandler<BulkDealerInvitationCommand, IDataResult<BulkInvitationJobDto>>
    {
        private readonly IBulkDealerInvitationService _bulkInvitationService;

        public BulkDealerInvitationCommandHandler(IBulkDealerInvitationService bulkInvitationService)
        {
            _bulkInvitationService = bulkInvitationService;
        }

        [SecuredOperation(Priority = 1)]
        public async Task<IDataResult<BulkInvitationJobDto>> Handle(
            BulkDealerInvitationCommand request,
            CancellationToken cancellationToken)
        {
            return await _bulkInvitationService.QueueBulkInvitationsAsync(
                request.ExcelFile,
                request.SponsorId,
                request.InvitationType,
                request.SendSms);
        }
    }
}
