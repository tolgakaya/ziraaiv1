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
    /// Advanced search query for users with multiple filter criteria
    /// Admin-only operation with flexible filtering
    /// </summary>
    public class SearchUsersQuery : IRequest<IDataResult<List<UserDetailDto>>>
    {
        public string SearchTerm { get; set; }
        public bool? IsActive { get; set; }
        public List<string> Roles { get; set; }
        public System.DateTime? RegisteredAfter { get; set; }
        public System.DateTime? RegisteredBefore { get; set; }
        public bool? HasSubscription { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string SortBy { get; set; } = "RecordDate";
        public bool SortDescending { get; set; } = true;

        public class SearchUsersQueryHandler : IRequestHandler<SearchUsersQuery, IDataResult<List<UserDetailDto>>>
        {
            private readonly IUserRepository _userRepository;
            private readonly IUserSubscriptionRepository _userSubscriptionRepository;

            public SearchUsersQueryHandler(
                IUserRepository userRepository,
                IUserSubscriptionRepository userSubscriptionRepository)
            {
                _userRepository = userRepository;
                _userSubscriptionRepository = userSubscriptionRepository;
            }

            [SecuredOperation(Priority = 1, Roles = "Admin")]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<List<UserDetailDto>>> Handle(SearchUsersQuery request, CancellationToken cancellationToken)
            {
                var query = _userRepository.Query();

                // Apply search term filter
                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    var searchLower = request.SearchTerm.ToLower();
                    query = query.Where(u =>
                        u.FullName.ToLower().Contains(searchLower) ||
                        u.Email.ToLower().Contains(searchLower) ||
                        u.MobilePhones.Contains(searchLower) ||
                        u.CitizenId.ToString().Contains(searchLower));
                }

                // Apply active status filter
                if (request.IsActive.HasValue)
                {
                    query = query.Where(u => u.IsActive == request.IsActive.Value);
                }

                // Apply date range filters
                if (request.RegisteredAfter.HasValue)
                {
                    query = query.Where(u => u.RecordDate >= request.RegisteredAfter.Value);
                }

                if (request.RegisteredBefore.HasValue)
                {
                    query = query.Where(u => u.RecordDate <= request.RegisteredBefore.Value);
                }

                // Get total count before pagination
                var totalCount = await query.CountAsync(cancellationToken);

                // Apply sorting
                query = request.SortBy switch
                {
                    "FullName" => request.SortDescending
                        ? query.OrderByDescending(u => u.FullName)
                        : query.OrderBy(u => u.FullName),
                    "Email" => request.SortDescending
                        ? query.OrderByDescending(u => u.Email)
                        : query.OrderBy(u => u.Email),
                    "RecordDate" => request.SortDescending
                        ? query.OrderByDescending(u => u.RecordDate)
                        : query.OrderBy(u => u.RecordDate),
                    _ => query.OrderByDescending(u => u.RecordDate)
                };

                // Apply pagination
                var users = await query
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync(cancellationToken);

                // Map to DTOs and apply role filter if specified
                var userDtos = new List<UserDetailDto>();
                foreach (var user in users)
                {
                    var roles = await _userRepository.GetUserGroupsAsync(user.UserId);

                    // Apply role filter
                    if (request.Roles != null && request.Roles.Any())
                    {
                        if (!roles.Any(r => request.Roles.Contains(r)))
                            continue;
                    }

                    // Apply subscription filter if specified
                    if (request.HasSubscription.HasValue)
                    {
                        var hasActiveSubscription = await _userSubscriptionRepository.GetAsync(s =>
                            s.UserId == user.UserId &&
                            s.IsActive &&
                            s.Status) != null;

                        if (request.HasSubscription.Value != hasActiveSubscription)
                            continue;
                    }

                    var claims = await _userRepository.GetClaimsAsync(user.UserId);

                    // Get deactivated by admin name if applicable
                    string deactivatedByName = null;
                    if (user.DeactivatedBy.HasValue)
                    {
                        var deactivatingAdmin = await _userRepository.GetAsync(u => u.UserId == user.DeactivatedBy.Value);
                        deactivatedByName = deactivatingAdmin?.FullName;
                    }

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
                        DeactivatedByName = deactivatedByName,
                        DeactivationReason = user.DeactivationReason,
                        Roles = roles,
                        Claims = claims.Select(c => c.Name).ToList()
                    });
                }

                return new SuccessDataResult<List<UserDetailDto>>(
                    userDtos,
                    $"Found {userDtos.Count} users matching search criteria (total: {totalCount})");
            }
        }
    }
}
