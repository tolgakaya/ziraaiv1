using System;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Business.Services.AdminAudit;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using MediatR;

namespace Business.Handlers.AdminSponsorship.Commands
{
    /// <summary>
    /// Admin command to deactivate a sponsorship code
    /// </summary>
    public class DeactivateCodeCommand : IRequest<IResult>
    {
        public int CodeId { get; set; }
        public string Reason { get; set; }

        // Admin context
        public int AdminUserId { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public string RequestPath { get; set; }

        public class DeactivateCodeCommandHandler : IRequestHandler<DeactivateCodeCommand, IResult>
        {
            private readonly ISponsorshipCodeRepository _codeRepository;
            private readonly IAdminAuditService _auditService;

            public DeactivateCodeCommandHandler(
                ISponsorshipCodeRepository codeRepository,
                IAdminAuditService auditService)
            {
                _codeRepository = codeRepository;
                _auditService = auditService;
            }

            [SecuredOperation(Priority = 1)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IResult> Handle(DeactivateCodeCommand request, CancellationToken cancellationToken)
            {
                var code = await _codeRepository.GetAsync(c => c.Id == request.CodeId);
                if (code == null)
                {
                    return new ErrorResult("Code not found");
                }

                if (!code.IsActive)
                {
                    return new ErrorResult("Code is already deactivated");
                }

                if (code.IsUsed)
                {
                    return new ErrorResult("Cannot deactivate a code that has already been used");
                }

                var beforeState = new
                {
                    code.IsActive,
                    code.Notes
                };

                code.IsActive = false;
                code.Notes = string.IsNullOrEmpty(code.Notes)
                    ? $"[Deactivated by Admin] {request.Reason}"
                    : $"{code.Notes}\n[Deactivated by Admin] {request.Reason}";

                _codeRepository.Update(code);
                await _codeRepository.SaveChangesAsync();

                // Audit log
                await _auditService.LogAsync(
                    action: "DeactivateCode",
                    adminUserId: request.AdminUserId,
                    targetUserId: code.SponsorId,
                    entityType: "SponsorshipCode",
                    entityId: code.Id,
                    isOnBehalfOf: false,
                    ipAddress: request.IpAddress,
                    userAgent: request.UserAgent,
                    requestPath: request.RequestPath,
                    reason: request.Reason,
                    beforeState: beforeState,
                    afterState: new
                    {
                        code.IsActive,
                        code.Notes
                    }
                );

                return new SuccessResult($"Code {code.Code} deactivated successfully");
            }
        }
    }
}
