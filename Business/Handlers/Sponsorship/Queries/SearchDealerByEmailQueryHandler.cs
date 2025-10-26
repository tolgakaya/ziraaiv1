using Business.BusinessAspects;
using Core.Utilities.Results;
using DataAccess.Abstract;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Sponsorship.Queries
{
    /// <summary>
    /// Handler for searching existing sponsor/dealer by email (Method A: Manual search)
    /// Authorization: Sponsor role only
    /// </summary>
    public class SearchDealerByEmailQueryHandler : IRequestHandler<SearchDealerByEmailQuery, IDataResult<DealerSearchResultDto>>
    {
        private readonly IUserRepository _userRepository;

        public SearchDealerByEmailQueryHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        [SecuredOperation(Priority = 1)]

        public async Task<IDataResult<DealerSearchResultDto>> Handle(SearchDealerByEmailQuery request, CancellationToken cancellationToken)
        {
            // Search for user by email
            var user = await _userRepository.GetAsync(u => u.Email == request.Email);

            if (user == null)
            {
                return new ErrorDataResult<DealerSearchResultDto>("No user found with this email address.");
            }

            // Check if user has Sponsor role
            var userGroups = await _userRepository.GetUserGroupsAsync(user.UserId);
            var isSponsor = userGroups.Any(g => g == "Sponsor");

            // Parse FullName into FirstName/LastName
            var nameParts = (user.FullName ?? "").Split(' ', 2);
            var firstName = nameParts.Length > 0 ? nameParts[0] : "";
            var lastName = nameParts.Length > 1 ? nameParts[1] : "";

            // Build result DTO
            var result = new DealerSearchResultDto
            {
                UserId = user.UserId,
                Email = user.Email,
                FirstName = firstName,
                LastName = lastName,
                CompanyName = "", // User entity doesn't have CompanyName
                IsSponsor = isSponsor
            };

            if (!isSponsor)
            {
                return new SuccessDataResult<DealerSearchResultDto>(
                    result, 
                    "User found but does not have Sponsor role. You can still transfer codes, but they will need Sponsor role to distribute them.");
            }

            return new SuccessDataResult<DealerSearchResultDto>(result, "Dealer found successfully.");
        }
    }
}
