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
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Business.Handlers.AdminUsers.Commands
{
    /// <summary>
    /// Command to deactivate a user account
    /// Admin-only operation with mandatory reason for audit trail
    /// </summary>
    public class DeactivateUserCommand : IRequest<IResult>
    {
        public int UserId { get; set; }
        public int AdminUserId { get; set; }
        public string Reason { get; set; }
        public HttpContext HttpContext { get; set; }

        public class DeactivateUserCommandHandler : IRequestHandler<DeactivateUserCommand, IResult>
        {
            private readonly IUserRepository _userRepository;
            private readonly IAdminAuditService _adminAuditService;

            public DeactivateUserCommandHandler(
                IUserRepository userRepository,
                IAdminAuditService adminAuditService)
            {
                _userRepository = userRepository;
                _adminAuditService = adminAuditService;
            }

            [SecuredOperation(Priority = 1, Roles = "Admin")]
            [LogAspect(typeof(FileLogger))]
            public async Task<IResult> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
            {
                // Validate reason
                if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length < 10)
                {
                    return new ErrorResult("Deactivation reason is required (minimum 10 characters)");
                }

                // Get target user
                var user = await _userRepository.GetAsync(u => u.UserId == request.UserId);
                if (user == null)
                {
                    return new ErrorResult("User not found");
                }

                // Check if user is already deactivated
                if (!user.IsActive)
                {
                    return new ErrorResult("User is already deactivated");
                }

                // Prevent deactivating admin users
                var userRoles = await _userRepository.GetUserGroupsAsync(user.UserId);
                if (userRoles.Contains("Admin"))
                {
                    return new ErrorResult("Cannot deactivate admin users");
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

                // Deactivate user
                user.IsActive = false;
                user.DeactivatedDate = DateTime.Now;
                user.DeactivatedBy = request.AdminUserId;
                user.DeactivationReason = request.Reason;

                _userRepository.Update(user);

                // Capture after state for audit
                var afterState = new
                {
                    user.UserId,
                    user.FullName,
                    user.Email,
                    user.IsActive,
                    user.DeactivatedDate,
                    user.DeactivatedBy,
                    user.DeactivationReason
                };

                // Log admin operation
                await _adminAuditService.LogUserManagementAsync(
                    adminUserId: request.AdminUserId,
                    action: "DEACTIVATE_USER",
                    httpContext: request.HttpContext,
                    targetUserId: user.UserId,
                    reason: request.Reason,
                    beforeState: beforeState,
                    afterState: afterState);

                return new SuccessResult($"User {user.FullName} has been deactivated successfully");
            }
        }
    }
}
