using System.Collections.Generic;
using System.Threading.Tasks;
// using Business.Handlers.Logs.Queries; // Temporarily disabled
using Core.Entities.Concrete;
using Core.Entities.Dtos; // LogDto is here
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Core.Utilities.Results;

namespace WebAPI.Controllers
{
    /// <summary>
    /// Logs controller for system log management
    /// Currently disabled - uncomment the handler references to enable
    /// </summary>
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class LogsController : BaseApiController
    {
        /// <summary>
        /// List Logs
        /// </summary>
        /// <remarks>Returns system logs (currently disabled)</remarks>
        /// <return>Logs List</return>
        /// <response code="200"></response>
        [Produces("application/json", "text/plain")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<LogDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        [HttpGet]
        public async Task<IActionResult> GetList()
        {
            // TODO: Uncomment below lines when Logs handler is ready
            // return GetResponseOnlyResultData(await Mediator.Send(new GetLogDtoQuery()));
            
            // Temporary response - returns empty list
            var emptyLogs = new List<LogDto>();
            var result = new SuccessDataResult<IEnumerable<LogDto>>(
                emptyLogs, 
                "Logs endpoint is currently disabled for maintenance"
            );
            
            return await Task.FromResult(GetResponseOnlyResultData(result));
        }
    }
}