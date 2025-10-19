using Business.Services.User;
using Core.Utilities.Results;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Users.Queries
{
    public class GetAvatarUrlQuery : IRequest<IDataResult<string>>
    {
        public int UserId { get; set; }

        public class GetAvatarUrlQueryHandler : IRequestHandler<GetAvatarUrlQuery, IDataResult<string>>
        {
            private readonly IAvatarService _avatarService;

            public GetAvatarUrlQueryHandler(IAvatarService avatarService)
            {
                _avatarService = avatarService;
            }

            public async Task<IDataResult<string>> Handle(GetAvatarUrlQuery request, CancellationToken cancellationToken)
            {
                return await _avatarService.GetAvatarUrlAsync(request.UserId);
            }
        }
    }
}
