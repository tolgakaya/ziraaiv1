using System;
using System.Collections.Generic;
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
    /// Admin command to bulk deactivate multiple users
    /// </summary>
    public class BulkDeactivateUsersCommand : IRequest<IResult>
    {
        public List<int> UserIds { get; set; }
        public string Reason { get; set; }

        // Admin context
        public int AdminUserId { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public string RequestPath { get; set; }

        public class BulkDeactivateUsersCommandHandler : IRequestHandler<BulkDeactivateUsersCommand, IResult>
        {
            private readonly IUserRepository _userRepository;
            private readonly IAdminAuditService _auditService;

            public BulkDeactivateUsersCommandHandler(
                IUserRepository userRepository,
                IAdminAuditService auditService)
            {
                _userRepository = userRepository;
                _auditService = auditService;
            }

            [SecuredOperation(Priority = 1)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IResult> Handle(BulkDeactivateUsersCommand request, CancellationToken cancellationToken)
            {
                if (request.UserIds == null || !request.UserIds.Any())
                {
                    return new ErrorResult("No user IDs provided");
                }

                var users = _userRepository.GetList(u => request.UserIds.Contains(u.UserId));
                
                if (!users.Any())
                {
                    return new ErrorResult("No users found with the provided IDs");
                }

                var deactivatedCount = 0;
                var alreadyDeactivated = 0;

                foreach (var user in users)
                {
                    if (!user.IsActive)
                    {
                        alreadyDeactivated++;
                        continue;
                    }

                    var beforeState = new { user.IsActive, user.DeactivatedDate };

                    user.IsActive = false;
                    user.DeactivatedDate = DateTime.Now;
                    user.DeactivatedBy = request.AdminUserId;
                    user.DeactivationReason = request.Reason;

                    _userRepository.Update(user);
                    deactivatedCount++;

                    // Audit log for each user
                    await _auditService.LogAsync(
                        action: "BulkDeactivateUser",
                        adminUserId: request.AdminUserId,
                        targetUserId: user.UserId,
                        entityType: "User",
                        entityId: user.UserId,
                        isOnBehalfOf: false,
                        ipAddress: request.IpAddress,
                        userAgent: request.UserAgent,
                        requestPath: request.RequestPath,
                        reason: request.Reason,
                        beforeState: beforeState,
                        afterState: new { user.IsActive, user.DeactivatedDate, user.DeactivatedBy }
                    );
                }

                await _userRepository.SaveChangesAsync();

                return new SuccessResult($"Bulk deactivation completed. Deactivated: {deactivatedCount}, Already inactive: {alreadyDeactivated}");
            }
        }
    }
}
