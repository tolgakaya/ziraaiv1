using Core.Utilities.Results;
using Entities.Dtos;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Business.Handlers.Sponsorship.Commands
{
    public class BulkDealerInvitationCommand : IRequest<IDataResult<BulkInvitationJobDto>>
    {
        public int SponsorId { get; set; }
        public IFormFile ExcelFile { get; set; }
        public string InvitationType { get; set; }  // "Invite" or "AutoCreate"
        public string DefaultTier { get; set; }      // S, M, L, XL (optional)
        public int DefaultCodeCount { get; set; }
        public bool SendSms { get; set; } = true;
        public bool UseRowSpecificCounts { get; set; } = false;
    }
}
