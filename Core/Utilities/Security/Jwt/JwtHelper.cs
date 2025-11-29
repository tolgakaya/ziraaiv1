using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using Core.Entities.Concrete;
using Core.Extensions;
using Core.Utilities.Security.Encyption;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;


namespace Core.Utilities.Security.Jwt
{
    public class JwtHelper : ITokenHelper
    {
        private readonly TokenOptions _tokenOptions;
        private DateTime _accessTokenExpiration;

        public JwtHelper(IConfiguration configuration)
        {
            Configuration = configuration;
            _tokenOptions = Configuration.GetSection("TokenOptions").Get<TokenOptions>();
        }

        public IConfiguration Configuration { get; }

        public static string DecodeToken(string input)
        {
            var handler = new JwtSecurityTokenHandler();
            if (input.StartsWith("Bearer "))
            {
                input = input["Bearer ".Length..];
            }

            return handler.ReadJwtToken(input).ToString();
        }

        public TAccessToken CreateToken<TAccessToken>(User user, List<string> userGroups = null)
            where TAccessToken : IAccessToken, new()
        {
            _accessTokenExpiration = DateTime.Now.AddMinutes(_tokenOptions.AccessTokenExpiration);
            var refreshTokenExpiration = DateTime.Now.AddMinutes(_tokenOptions.RefreshTokenExpiration);
            var securityKey = SecurityKeyHelper.CreateSecurityKey(_tokenOptions.SecurityKey);
            var signingCredentials = SigningCredentialsHelper.CreateSigningCredentials(securityKey);
            var jwt = CreateJwtSecurityToken(_tokenOptions, user, signingCredentials, null, userGroups);
            var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
            var token = jwtSecurityTokenHandler.WriteToken(jwt);

            return new TAccessToken()
            {
                Token = token,
                Expiration = _accessTokenExpiration,
                RefreshToken = GenerateRefreshToken(),
                RefreshTokenExpiration = refreshTokenExpiration
            };
        }

        public JwtSecurityToken CreateJwtSecurityToken(
            TokenOptions tokenOptions,
            User user,
            SigningCredentials signingCredentials,
            IEnumerable<OperationClaim> operationClaims = null,
            List<string> userGroups = null)
        {
            var jwt = new JwtSecurityToken(
                tokenOptions.Issuer,
                tokenOptions.Audience,
                expires: _accessTokenExpiration,
                notBefore: DateTime.Now,
                claims: SetClaims(user, operationClaims, userGroups),
                signingCredentials: signingCredentials);
            return jwt;
        }
        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];

            using var generator = RandomNumberGenerator.Create();
            generator.GetBytes(randomNumber);

            return Convert.ToBase64String(randomNumber);
        }
        private static IEnumerable<Claim> SetClaims(User user, IEnumerable<OperationClaim> operationClaims = null, List<string> userGroups = null)
        {
            var claims = new List<Claim>();
            claims.AddNameIdentifier(user.UserId.ToString());
            if (user.CitizenId > 0)
            {
                claims.AddNameUniqueIdentifier(user.CitizenId.ToString());
            }

            if (!string.IsNullOrEmpty(user.FullName))
            {
                claims.AddName($"{user.FullName}");
            }

            // Add email if available (for email-based login)
            if (!string.IsNullOrEmpty(user.Email))
            {
                claims.Add(new Claim(ClaimTypes.Email, user.Email));
            }

            // Add mobile phone if available (for phone-based login)
            if (!string.IsNullOrEmpty(user.MobilePhones))
            {
                claims.Add(new Claim(ClaimTypes.MobilePhone, user.MobilePhones));
            }

            // Add user groups as roles
            if (userGroups != null && userGroups.Any())
            {
                foreach (var group in userGroups)
                {
                    claims.Add(new Claim(ClaimTypes.Role, group));
                }
            }
            else if (!string.IsNullOrEmpty(user.AuthenticationProviderType))
            {
                // Fallback to AuthenticationProviderType if no groups
                claims.Add(new Claim(ClaimTypes.Role, user.AuthenticationProviderType));
            }

            // Add operation claims if provided
            if (operationClaims != null)
            {
                foreach (var claim in operationClaims)
                {
                    claims.Add(new Claim("permissions", claim.Name));
                }
            }

            return claims;
        }
    }
}