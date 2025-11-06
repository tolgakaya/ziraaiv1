using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Business.Constants;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using Core.Utilities.Security.Hashing;
using DataAccess.Abstract;
using MediatR;

namespace Business.Handlers.Users.Commands
{
    public class UserChangePasswordCommand : IRequest<IResult>
    {
        public int UserId { get; set; }
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }

        public class UserChangePasswordCommandHandler : IRequestHandler<UserChangePasswordCommand, IResult>
        {
            private readonly IUserRepository _userRepository;
            private readonly IMediator _mediator;

            public UserChangePasswordCommandHandler(IUserRepository userRepository, IMediator mediator)
            {
                _userRepository = userRepository;
                _mediator = mediator;
            }

            [SecuredOperation(Priority = 1)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IResult> Handle(UserChangePasswordCommand request, CancellationToken cancellationToken)
            {
                var user = await _userRepository.GetAsync(u => u.UserId == request.UserId);
                if (user == null)
                {
                    return new ErrorResult(Messages.UserNotFound);
                }

                // Verify old password
                if (!HashingHelper.VerifyPasswordHash(request.OldPassword, user.PasswordSalt, user.PasswordHash))
                {
                    return new ErrorResult(Messages.PasswordError);
                }

                // Create new password hash
                HashingHelper.CreatePasswordHash(request.NewPassword, out var passwordSalt, out var passwordHash);

                user.PasswordHash = passwordHash;
                user.PasswordSalt = passwordSalt;

                _userRepository.Update(user);
                await _userRepository.SaveChangesAsync();
                return new SuccessResult(Messages.PasswordChanged);
            }
        }
    }
}