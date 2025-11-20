using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Business.Handlers.Farmers.Queries;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Handlers.Farmers.Queries
{
    /// <summary>
    /// Get farmer profile by userId (from JWT token)
    /// </summary>
    public class GetFarmerProfileQuery : IRequest<IDataResult<FarmerProfileDto>>
    {
        public int UserId { get; set; }

        public class GetFarmerProfileQueryHandler : IRequestHandler<GetFarmerProfileQuery, IDataResult<FarmerProfileDto>>
        {
            private readonly IUserRepository _userRepository;

            public GetFarmerProfileQueryHandler(IUserRepository userRepository)
            {
                _userRepository = userRepository;
            }

            public async Task<IDataResult<FarmerProfileDto>> Handle(GetFarmerProfileQuery request, CancellationToken cancellationToken)
            {
                var user = await _userRepository.Query()
                    .Where(u => u.UserId == request.UserId)
                    .Select(u => new FarmerProfileDto
                    {
                        UserId = u.UserId,
                        CitizenId = u.CitizenId,
                        FullName = u.FullName,
                        Email = u.Email,
                        MobilePhones = u.MobilePhones,
                        BirthDate = u.BirthDate,
                        Gender = u.Gender,
                        Address = u.Address,
                        Notes = u.Notes,
                        Status = u.Status,
                        IsActive = u.IsActive,
                        RecordDate = u.RecordDate,
                        UpdateContactDate = u.UpdateContactDate,
                        AvatarUrl = u.AvatarUrl,
                        AvatarThumbnailUrl = u.AvatarThumbnailUrl,
                        AvatarUpdatedDate = u.AvatarUpdatedDate,
                        RegistrationReferralCode = u.RegistrationReferralCode,
                        DeactivatedDate = u.DeactivatedDate,
                        DeactivationReason = u.DeactivationReason
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (user == null)
                {
                    return new ErrorDataResult<FarmerProfileDto>("Kullanıcı bulunamadı.");
                }

                return new SuccessDataResult<FarmerProfileDto>(user);
            }
        }
    }
}
