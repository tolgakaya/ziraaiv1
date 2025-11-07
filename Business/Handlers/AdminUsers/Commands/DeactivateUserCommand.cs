using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Business.Services.AdminAudit;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using MediatR;

namespace Business.Handlers.AdminUsers.Commands
{
    /// <summary>
    /// Admin command to deactivate a user account
    /// </summary>
    public class DeactivateUserCommand : IRequest<IResult>
    {
        public int UserId { get; set; }
        public int AdminUserId { get; set; }
        public string Reason { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public string RequestPath { get; set; }

        public class DeactivateUserCommandHandler : IRequestHandler<DeactivateUserCommand, IResult>
        {
            private readonly IUserRepository _userRepository;
            private readonly IUserGroupRepository _userGroupRepository;
            private readonly IAdminAuditService _auditService;

            public DeactivateUserCommandHandler(
                IUserRepository userRepository,
                IUserGroupRepository userGroupRepository,
                IAdminAuditService auditService)
            {
                _userRepository = userRepository;
                _userGroupRepository = userGroupRepository;
                _auditService = auditService;
            }

            [SecuredOperation(Priority = 1)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IResult> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
            {
                // SECURITY: Prevent deactivating Admin users
                // Admins should not be able to deactivate other admin accounts
                var isAdminUser = _userGroupRepository.Query()
                    .Any(ug => ug.UserId == request.UserId && ug.GroupId == 1); // GroupId 1 = Admin role
                
                if (isAdminUser)
                {
                    return new ErrorResult("Access denied: Cannot deactivate admin users");
                }

                var user = await _userRepository.GetAsync(u => u.UserId == request.UserId);

                if (user == null)
                {
                    return new ErrorResult("User not found");
                }

                if (!user.IsActive)
                {
                    return new ErrorResult("User is already deactivated");
                }

                // Store before state
                var beforeState = new { user.IsActive, user.DeactivatedDate, user.DeactivatedBy };

                // Deactivate user
                user.IsActive = false;
                user.DeactivatedDate = DateTime.Now;
                user.DeactivatedBy = request.AdminUserId;
                user.DeactivationReason = request.Reason;

                _userRepository.Update(user);
                await _userRepository.SaveChangesAsync();

                // Audit log
                await _auditService.LogAsync(
                    action: "DeactivateUser",
                    adminUserId: request.AdminUserId,
                    targetUserId: request.UserId,
                    entityType: "User",
                    entityId: request.UserId,
                    isOnBehalfOf: false,
                    ipAddress: request.IpAddress,
                    userAgent: request.UserAgent,
                    requestPath: request.RequestPath,
                    reason: request.Reason,
                    beforeState: beforeState,
                    afterState: new { user.IsActive, user.DeactivatedDate, user.DeactivatedBy }
                );

                return new SuccessResult($"User {user.Email} has been deactivated");
            }
        }
    }
}
