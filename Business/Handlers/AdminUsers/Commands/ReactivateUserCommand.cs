using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Business.Services.AdminAudit;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Business.Handlers.AdminUsers.Commands
{
    /// <summary>
    /// Command to reactivate a deactivated user account
    /// Admin-only operation with reason required for audit trail
    /// </summary>
    public class ReactivateUserCommand : IRequest<IResult>
    {
        public int UserId { get; set; }
        public int AdminUserId { get; set; }
        public string Reason { get; set; }
        public HttpContext HttpContext { get; set; }

        public class ReactivateUserCommandHandler : IRequestHandler<ReactivateUserCommand, IResult>
        {
            private readonly IUserRepository _userRepository;
            private readonly IAdminAuditService _adminAuditService;

            public ReactivateUserCommandHandler(
                IUserRepository userRepository,
                IAdminAuditService adminAuditService)
            {
                _userRepository = userRepository;
                _adminAuditService = adminAuditService;
            }

            [SecuredOperation(Priority = 1, Roles = "Admin")]
            [LogAspect(typeof(FileLogger))]
            public async Task<IResult> Handle(ReactivateUserCommand request, CancellationToken cancellationToken)
            {
                // Validate reason
                if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length < 10)
                {
                    return new ErrorResult("Reactivation reason is required (minimum 10 characters)");
                }

                // Get target user
                var user = await _userRepository.GetAsync(u => u.UserId == request.UserId);
                if (user == null)
                {
                    return new ErrorResult("User not found");
                }

                // Check if user is already active
                if (user.IsActive)
                {
                    return new ErrorResult("User is already active");
                }

                // Capture before state for audit
                var beforeState = new
                {
                    user.UserId,
                    user.FullName,
                    user.Email,
                    user.IsActive,
                    user.DeactivatedDate,
                    user.DeactivatedBy,
                    user.DeactivationReason
                };

                // Reactivate user
                user.IsActive = true;
                user.DeactivatedDate = null;
                user.DeactivatedBy = null;
                user.DeactivationReason = null;

                _userRepository.Update(user);

                // Capture after state for audit
                var afterState = new
                {
                    user.UserId,
                    user.FullName,
                    user.Email,
                    user.IsActive,
                    DeactivatedDate = (System.DateTime?)null,
                    DeactivatedBy = (int?)null,
                    DeactivationReason = (string)null
                };

                // Log admin operation
                await _adminAuditService.LogUserManagementAsync(
                    adminUserId: request.AdminUserId,
                    action: "REACTIVATE_USER",
                    httpContext: request.HttpContext,
                    targetUserId: user.UserId,
                    reason: request.Reason,
                    beforeState: beforeState,
                    afterState: afterState);

                return new SuccessResult($"User {user.FullName} has been reactivated successfully");
            }
        }
    }
}
