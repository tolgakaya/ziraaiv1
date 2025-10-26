using System.Collections.Generic;
using System.Linq;
using System.Security;
using Business.Constants;
using Castle.DynamicProxy;
using Core.CrossCuttingConcerns.Caching;
using Core.Utilities.Interceptors;
using Core.Utilities.IoC;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Business.BusinessAspects
{
    /// <summary>
    /// This Aspect control the user's roles in HttpContext by inject the IHttpContextAccessor.
    /// It is checked by writing as [SecuredOperation] on the handler.
    /// If a valid authorization cannot be found in aspect, it throws an exception.
    /// </summary>
    public class SecuredOperation : MethodInterception
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ICacheManager _cacheManager;


        public SecuredOperation()
        {
            _httpContextAccessor = ServiceTool.ServiceProvider.GetService<IHttpContextAccessor>();
            _cacheManager = ServiceTool.ServiceProvider.GetService<ICacheManager>();
        }

        protected override void OnBefore(IInvocation invocation)
        {
            var userId = _httpContextAccessor.HttpContext?.User.Claims
                .FirstOrDefault(x => x.Type.EndsWith("nameidentifier"))?.Value;

            if (userId == null)
            {
                throw new SecurityException(Messages.AuthorizationsDenied);
            }

            var oprClaims = _cacheManager.Get<IEnumerable<string>>($"{CacheKeys.UserIdForClaim}={userId}");

            // Get operation name from handler class name
            // Example: "TransferCodesToDealerCommandHandler" -> "TransferCodesToDealerCommand"
            var operationName = invocation.Method?.DeclaringType?.Name;

            if (string.IsNullOrEmpty(operationName))
            {
                throw new SecurityException(Messages.AuthorizationsDenied);
            }

            // Remove only "Handler" suffix to match OperationClaim naming convention
            // Claims are stored as: "CreateUserCommand", "GetUsersQuery", etc. (without "Handler")
            operationName = operationName.Replace("Handler", "");

            // DEBUG: Log for troubleshooting
            var logger = ServiceTool.ServiceProvider.GetService<ILogger<SecuredOperation>>();
            logger?.LogInformation($"[SecuredOperation] UserId: {userId}, Operation: {operationName}, CachedClaims: {(oprClaims != null ? string.Join(", ", oprClaims) : "NULL")}");

            // If operation claims exist and contain this operation, allow access
            if (oprClaims != null && oprClaims.Contains(operationName))
            {
                return;
            }

            throw new SecurityException(Messages.AuthorizationsDenied);
        }
    }
}