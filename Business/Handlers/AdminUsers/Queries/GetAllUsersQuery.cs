using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Handlers.AdminUsers.Queries
{
    /// <summary>
    /// Query to get all users with pagination and optional filtering
    /// Admin-only operation with comprehensive user details
    /// </summary>
    public class GetAllUsersQuery : IRequest<IDataResult<List<UserDetailDto>>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public bool? IsActive { get; set; }
        public string Role { get; set; }
        public string SearchTerm { get; set; }

        public class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, IDataResult<List<UserDetailDto>>>
        {
            private readonly IUserRepository _userRepository;

            public GetAllUsersQueryHandler(IUserRepository userRepository)
            {
                _userRepository = userRepository;
            }

            [SecuredOperation(Priority = 1, Roles = "Admin")]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<List<UserDetailDto>>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
            {
                // Build query with filters
                var query = _userRepository.Query();

                // Filter by active status
                if (request.IsActive.HasValue)
                {
                    query = query.Where(u => u.IsActive == request.IsActive.Value);
                }

                // Filter by search term (name, email, phone)
                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    var searchLower = request.SearchTerm.ToLower();
                    query = query.Where(u =>
                        u.FullName.ToLower().Contains(searchLower) ||
                        u.Email.ToLower().Contains(searchLower) ||
                        u.MobilePhones.Contains(searchLower));
                }

                // Get total count before pagination
                var totalCount = await query.CountAsync(cancellationToken);

                // Apply pagination
                var users = await query
                    .OrderByDescending(u => u.RecordDate)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync(cancellationToken);

                // Map to DTOs with roles
                var userDtos = new List<UserDetailDto>();
                foreach (var user in users)
                {
                    var roles = await _userRepository.GetUserGroupsAsync(user.UserId);
                    var claims = await _userRepository.GetClaimsAsync(user.UserId);

                    userDtos.Add(new UserDetailDto
                    {
                        UserId = user.UserId,
                        CitizenId = user.CitizenId,
                        FullName = user.FullName,
                        Email = user.Email,
                        MobilePhones = user.MobilePhones,
                        Status = user.Status,
                        BirthDate = user.BirthDate,
                        Gender = user.Gender,
                        RecordDate = user.RecordDate,
                        Address = user.Address,
                        Notes = user.Notes,
                        UpdateContactDate = user.UpdateContactDate,
                        RegistrationReferralCode = user.RegistrationReferralCode,
                        AvatarUrl = user.AvatarUrl,
                        AvatarThumbnailUrl = user.AvatarThumbnailUrl,
                        AvatarUpdatedDate = user.AvatarUpdatedDate,
                        IsActive = user.IsActive,
                        DeactivatedDate = user.DeactivatedDate,
                        DeactivatedBy = user.DeactivatedBy,
                        DeactivationReason = user.DeactivationReason,
                        Roles = roles,
                        Claims = claims.Select(c => c.Name).ToList()
                    });
                }

                return new SuccessDataResult<List<UserDetailDto>>(
                    userDtos,
                    $"Retrieved {userDtos.Count} users out of {totalCount} total");
            }
        }
    }
}
