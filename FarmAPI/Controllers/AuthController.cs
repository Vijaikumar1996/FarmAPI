using FarmAPI.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static FarmAPI.DTOs.LoginDto;

namespace FarmAPI.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(
            IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequestDto request)
        {
            var result =
                await _authService
                    .LoginAsync(request);

            if (result == null)
                return Unauthorized(
                    new
                    {
                        Message =
                            "Invalid username or password"
                    });

            return Ok(result);
        }
    }
}
