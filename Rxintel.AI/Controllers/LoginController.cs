using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rxintel.Generic.Api;
using Rxintel.Generic.Common.Exceptions;
using Rxintel.Generic.Common.Implementations.Responses;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Services.Interfaces;

namespace Rxintel.AI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : BaseController
    {
        private readonly ILoginService _loginService;

        public LoginController(ILoginService loginService)
        {
            _loginService = loginService;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<SuccessResult<AdminDto>> Register([FromBody] AdminDto adminDto)
        {
            return await _loginService.RegisterWithMessage(adminDto);
        }

        [AllowAnonymous]
        [HttpPost("login")]
        [ProducesResponseType(typeof(TokenDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<TokenDto>> Login([FromBody] AdminDto adminDto)
        {
            if(!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var tokenDto = await _loginService.LoginAsync(
                    adminDto.Email ?? string.Empty,
                    adminDto.Password ?? string.Empty);
                return Ok(tokenDto);
            }
            catch (WebApiException ex)
            {
                return StatusCode((int)ex.HttpStatusCode, ex.ErrorMessage);
            }
        }

        [HttpPost("refresh")]
        [ProducesResponseType(typeof(TokenDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<TokenDto>> Refresh()
        {
            try
            {
                var tokenDto = await _loginService.RefreshAsync();
                return Ok(tokenDto);
            }
            catch (WebApiException ex)
            {
                return StatusCode((int)ex.HttpStatusCode, ex.ErrorMessage);
            }
        }
    }
}
