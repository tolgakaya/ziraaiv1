using Business.Services.User;
using Core.Utilities.Results;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Users.Commands
{
    public class UploadAvatarCommand : IRequest<IDataResult<AvatarUploadResult>>
    {
        public int UserId { get; set; }
        public IFormFile File { get; set; }

        public class UploadAvatarCommandHandler : IRequestHandler<UploadAvatarCommand, IDataResult<AvatarUploadResult>>
        {
            private readonly IAvatarService _avatarService;

            public UploadAvatarCommandHandler(IAvatarService avatarService)
            {
                _avatarService = avatarService;
            }

            public async Task<IDataResult<AvatarUploadResult>> Handle(UploadAvatarCommand request, CancellationToken cancellationToken)
            {
                return await _avatarService.UploadAvatarAsync(request.UserId, request.File);
            }
        }
    }
}
