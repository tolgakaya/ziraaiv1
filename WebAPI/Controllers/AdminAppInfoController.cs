using System.Security.Claims;
using System.Threading.Tasks;
using Business.Handlers.AppInfos.Commands;
using Business.Handlers.AppInfos.Queries;
using Entities.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    /// <summary>
    /// Admin app info management endpoints (About Us page management)
    /// </summary>
    public class AdminAppInfoController : AdminBaseController
    {
        /// <summary>
        /// Get app info with metadata
        /// </summary>
        /// <returns>App info with admin metadata (created date, updated by user, etc.)</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AdminAppInfoDto))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAppInfo()
        {
            var result = await Mediator.Send(new GetAppInfoAsAdminQuery());
            return result.Success ? Ok(result) : NotFound(result);
        }

        /// <summary>
        /// Update app info (creates if not exists)
        /// </summary>
        /// <param name="dto">App info data to update</param>
        /// <returns>Success or error result</returns>
        [HttpPut]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateAppInfo([FromBody] UpdateAppInfoDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
            {
                return Unauthorized(new { message = "Geçersiz kullanıcı token'ı." });
            }

            var command = new UpdateAppInfoCommand
            {
                UserId = userId,
                CompanyName = dto.CompanyName,
                CompanyDescription = dto.CompanyDescription,
                AppVersion = dto.AppVersion,
                Address = dto.Address,
                Email = dto.Email,
                Phone = dto.Phone,
                WebsiteUrl = dto.WebsiteUrl,
                FacebookUrl = dto.FacebookUrl,
                InstagramUrl = dto.InstagramUrl,
                YouTubeUrl = dto.YouTubeUrl,
                TwitterUrl = dto.TwitterUrl,
                LinkedInUrl = dto.LinkedInUrl,
                TermsOfServiceUrl = dto.TermsOfServiceUrl,
                PrivacyPolicyUrl = dto.PrivacyPolicyUrl,
                CookiePolicyUrl = dto.CookiePolicyUrl
            };

            var result = await Mediator.Send(command);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
