using System.Collections.Generic;
using Core.Entities.Concrete;

namespace Core.Utilities.Security.Jwt
{
    public interface ITokenHelper
    {
        TAccessToken CreateToken<TAccessToken>(User user, List<string> userGroups = null)
          where TAccessToken : IAccessToken, new();

        string GenerateRefreshToken();
    }
}