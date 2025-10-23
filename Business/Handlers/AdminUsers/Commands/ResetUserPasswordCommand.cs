using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Business.Services.AdminAudit;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using Core.Utilities.Security.Hashing;
using DataAccess.Abstract;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Business.Handlers.AdminUsers.Commands
{
    /// <summary>
    /// Command to reset a user's password to a temporary password
    /// Admin-only operation with mandatory reason for audit trail
    /// </summary>
    public class ResetUserPasswordCommand : IRequest<IDataResult<string>>
    {
        public int UserId { get; set; }
        public int AdminUserId { get; set; }
        public string Reason { get; set; }
        public string NewPassword { get; set; }  // Optional: If not provided, generates random password
        public HttpContext HttpContext { get; set; }

        public class ResetUserPasswordCommandHandler : IRequestHandler<ResetUserPasswordCommand, IDataResult<string>>
        {
            private readonly IUserRepository _userRepository;
            private readonly IAdminAuditService _adminAuditService;

            public ResetUserPasswordCommandHandler(
                IUserRepository userRepository,
                IAdminAuditService adminAuditService)
            {
                _userRepository = userRepository;
                _adminAuditService = adminAuditService;
            }

            [SecuredOperation(Priority = 1, Roles = "Admin")]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<string>> Handle(ResetUserPasswordCommand request, CancellationToken cancellationToken)
            {
                // Validate reason
                if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length < 10)
                {
                    return new ErrorDataResult<string>("Password reset reason is required (minimum 10 characters)");
                }

                // Get target user
                var user = await _userRepository.GetAsync(u => u.UserId == request.UserId);
                if (user == null)
                {
                    return new ErrorDataResult<string>("User not found");
                }

                // Prevent resetting admin passwords
                var userRoles = await _userRepository.GetUserGroupsAsync(user.UserId);
                if (userRoles.Contains("Admin"))
                {
                    return new ErrorDataResult<string>("Cannot reset admin user passwords");
                }

                // Generate temporary password if not provided
                string temporaryPassword = request.NewPassword;
                if (string.IsNullOrWhiteSpace(temporaryPassword))
                {
                    temporaryPassword = GenerateTemporaryPassword();
                }

                // Hash the new password
                HashingHelper.CreatePasswordHash(temporaryPassword, out byte[] passwordHash, out byte[] passwordSalt);

                // Capture before state (without password hash for security)
                var beforeState = new
                {
                    user.UserId,
                    user.FullName,
                    user.Email,
                    PasswordChanged = false
                };

                // Update user password
                user.PasswordHash = passwordHash;
                user.PasswordSalt = passwordSalt;
                user.RefreshToken = null; // Invalidate existing sessions

                _userRepository.Update(user);

                // Capture after state
                var afterState = new
                {
                    user.UserId,
                    user.FullName,
                    user.Email,
                    PasswordChanged = true,
                    SessionsInvalidated = true
                };

                // Log admin operation (without exposing password)
                await _adminAuditService.LogUserManagementAsync(
                    adminUserId: request.AdminUserId,
                    action: "RESET_USER_PASSWORD",
                    httpContext: request.HttpContext,
                    targetUserId: user.UserId,
                    reason: request.Reason,
                    beforeState: beforeState,
                    afterState: afterState);

                return new SuccessDataResult<string>(
                    temporaryPassword,
                    $"Password for user {user.FullName} has been reset successfully. Temporary password: {temporaryPassword}");
            }

            /// <summary>
            /// Generate a secure temporary password
            /// Format: 2 uppercase + 2 digits + 2 lowercase + 2 special chars = 8 characters
            /// </summary>
            private string GenerateTemporaryPassword()
            {
                const string uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ"; // Excluding I, O
                const string lowercase = "abcdefghijkmnopqrstuvwxyz"; // Excluding l
                const string digits = "23456789"; // Excluding 0, 1
                const string special = "!@#$%&*";

                var random = new System.Random();
                
                var password = new char[8];
                password[0] = uppercase[random.Next(uppercase.Length)];
                password[1] = uppercase[random.Next(uppercase.Length)];
                password[2] = digits[random.Next(digits.Length)];
                password[3] = digits[random.Next(digits.Length)];
                password[4] = lowercase[random.Next(lowercase.Length)];
                password[5] = lowercase[random.Next(lowercase.Length)];
                password[6] = special[random.Next(special.Length)];
                password[7] = special[random.Next(special.Length)];

                // Shuffle the password for better randomness
                return new string(Shuffle(password));
            }

            /// <summary>
            /// Shuffle array using Fisher-Yates algorithm
            /// </summary>
            private char[] Shuffle(char[] array)
            {
                var random = new System.Random();
                for (int i = array.Length - 1; i > 0; i--)
                {
                    int j = random.Next(i + 1);
                    var temp = array[i];
                    array[i] = array[j];
                    array[j] = temp;
                }
                return array;
            }
        }
    }
}
