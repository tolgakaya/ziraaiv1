using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using MediatR;

namespace Business.Handlers.AdminSponsorship.Queries
{
    /// <summary>
    /// Admin query to get sponsorship code by ID with full details
    /// </summary>
    public class GetCodeByIdQuery : IRequest<IDataResult<SponsorshipCode>>
    {
        public int CodeId { get; set; }

        public class GetCodeByIdQueryHandler : IRequestHandler<GetCodeByIdQuery, IDataResult<SponsorshipCode>>
        {
            private readonly ISponsorshipCodeRepository _codeRepository;

            public GetCodeByIdQueryHandler(ISponsorshipCodeRepository codeRepository)
            {
                _codeRepository = codeRepository;
            }

            [SecuredOperation(Priority = 1)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<SponsorshipCode>> Handle(GetCodeByIdQuery request, CancellationToken cancellationToken)
            {
                var code = await _codeRepository.GetAsync(c => c.Id == request.CodeId);

                if (code == null)
                {
                    return new ErrorDataResult<SponsorshipCode>("Code not found");
                }

                return new SuccessDataResult<SponsorshipCode>(code, "Code retrieved successfully");
            }
        }
    }
}
