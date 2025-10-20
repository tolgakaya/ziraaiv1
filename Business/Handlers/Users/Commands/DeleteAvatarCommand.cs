using Business.Services.User;
using Core.Utilities.Results;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Users.Commands
{
    public class DeleteAvatarCommand : IRequest<IResult>
    {
        public int UserId { get; set; }

        public class DeleteAvatarCommandHandler : IRequestHandler<DeleteAvatarCommand, IResult>
        {
            private readonly IAvatarService _avatarService;

            public DeleteAvatarCommandHandler(IAvatarService avatarService)
            {
                _avatarService = avatarService;
            }

            public async Task<IResult> Handle(DeleteAvatarCommand request, CancellationToken cancellationToken)
            {
                return await _avatarService.DeleteAvatarAsync(request.UserId);
            }
        }
    }
}
