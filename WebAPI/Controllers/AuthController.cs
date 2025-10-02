using System.Threading.Tasks;
using Business.Handlers.Authorizations.Commands;
using Business.Handlers.Authorizations.Queries;
using Business.Handlers.Users.Commands;
using Business.Services.Authentication;
using Core.Utilities.Results;
using Core.Utilities.Security.Jwt;
using Core.Entities.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Entities.Dtos;
using IResult = Core.Utilities.Results.IResult;

namespace WebAPI.Controllers
{
    /// <summary>
    /// Make it Authorization operations
    /// </summary>
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class AuthController : BaseApiController
    {

        /// <summary>
        /// Make it User Login operations
        /// </summary>
        /// <param name="loginModel"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [Consumes("application/json")]
        [Produces("application/json", "text/plain")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IDataResult<AccessToken>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginUserQuery loginModel)
        {
            var result = await Mediator.Send(loginModel);
            return result.Success ? Ok(result) : Unauthorized(result.Message);
        }

        [AllowAnonymous]
        [Consumes("application/json")]
        [Produces("application/json", "text/plain")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IDataResult<AccessToken>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(string))]
        [HttpPost("refresh-token")]
        public async Task<IActionResult> LoginWithRefreshToken([FromBody] LoginWithRefreshTokenQuery command)
        {
            var result = await Mediator.Send(command);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        ///  Make it User Register operations
        /// </summary>
        /// <param name="createUser"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [Consumes("application/json")]
        [Produces("application/json", "text/plain")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IResult))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(IResult))]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserCommand createUser)
        {
            return GetResponseOnlyResult(await Mediator.Send(createUser));
        }

        /// <summary>
        /// Make it Forgot Password operations
        /// </summary>
        /// <remarks>tckimlikno</remarks>
        /// <return></return>
        /// <response code="200"></response>
        [AllowAnonymous]
        [Consumes("application/json")]
        [Produces("application/json", "text/plain")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IResult))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(IResult))]
        [HttpPut("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand forgotPassword)
        {
            return GetResponseOnlyResult(await Mediator.Send(forgotPassword));
        }

        /// <summary>
        /// Make it Change Password operation
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        [Consumes("application/json")]
        [Produces("application/json", "text/plain")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        [HttpPut("user-password")]
        public async Task<IActionResult> ChangeUserPassword([FromBody] UserChangePasswordCommand command)
        {
            return GetResponseOnlyResultMessage(await Mediator.Send(command));
        }

        /// <summary>
        /// Mobile Login
        /// </summary>
        /// <param name="verifyCid"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [Consumes("application/json")]
        [Produces("application/json", "text/plain")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        [HttpPost("verify")]
        public async Task<IActionResult> Verification([FromBody] VerifyCidQuery verifyCid)
        {
            return GetResponseOnlyResultMessage(await Mediator.Send(verifyCid));
        }

        /// <summary>
        /// Token decode test
        /// </summary>
        /// <returns></returns>
        [Consumes("application/json")]
        [Produces("application/json", "text/plain")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [HttpPost("test")]
        public IActionResult LoginTest()
        {
            var auth = Request.Headers["Authorization"];
            var token = JwtHelper.DecodeToken(auth);

            return Ok(token);
        }

        /// <summary>
        /// Register with phone number (OTP-based authentication)
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [Consumes("application/json")]
        [Produces("application/json", "text/plain")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IResult))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(IResult))]
        [HttpPost("register-phone")]
        public async Task<IActionResult> RegisterWithPhone([FromBody] Business.Handlers.Authorizations.Commands.RegisterUserWithPhoneCommand command)
        {
            return GetResponseOnlyResult(await Mediator.Send(command));
        }

        /// <summary>
        /// Login with phone number - sends OTP via SMS
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [Consumes("application/json")]
        [Produces("application/json", "text/plain")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IDataResult<Business.Services.Authentication.Model.LoginUserResult>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        [HttpPost("login-phone")]
        public async Task<IActionResult> LoginWithPhone([FromBody] LoginWithPhoneRequest request)
        {
            var command = new Business.Services.Authentication.Model.LoginUserCommand
            {
                MobilePhone = request.MobilePhone,
                Provider = Core.Entities.Concrete.AuthenticationProviderType.Phone
            };

            var result = await Mediator.Send(command);
            return result.Success ? Ok(result) : BadRequest(result.Message);
        }

        /// <summary>
        /// Verify phone OTP and get JWT token
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [Consumes("application/json")]
        [Produces("application/json", "text/plain")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IDataResult<DArchToken>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        [HttpPost("verify-phone-otp")]
        public async Task<IActionResult> VerifyPhoneOtp([FromBody] VerifyPhoneOtpRequest request)
        {
            var command = new Business.Services.Authentication.Model.VerifyOtpCommand
            {
                ExternalUserId = request.MobilePhone,
                Code = request.Code,
                Provider = Core.Entities.Concrete.AuthenticationProviderType.Phone
            };

            var result = await Mediator.Send(command);
            return result.Success ? Ok(result) : BadRequest(result.Message);
        }
    }

    /// <summary>
    /// Login with phone request DTO
    /// </summary>
    public class LoginWithPhoneRequest
    {
        public string MobilePhone { get; set; }
    }

    /// <summary>
    /// Verify phone OTP request DTO
    /// </summary>
    public class VerifyPhoneOtpRequest
    {
        public string MobilePhone { get; set; }
        public int Code { get; set; }
    }
}