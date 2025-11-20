using System.Threading.Tasks;
using Business.Handlers.AppInfos.Queries;
using Entities.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    /// <summary>
    /// App info endpoint for Farmers and Sponsors (About Us page)
    /// </summary>
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class AppInfoController : BaseApiController
    {
        /// <summary>
        /// Get app info (About Us page data)
        /// </summary>
        /// <returns>App info including company details, contact info, social media links, and legal page URLs</returns>
        [Authorize(Roles = "Farmer,Sponsor")]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AppInfoDto))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAppInfo()
        {
            var result = await Mediator.Send(new GetAppInfoQuery());
            return result.Success ? Ok(result) : NotFound(result);
        }
    }
}
