using DeliveryAPI.Api.Contracts.Request;
using DeliveryAPI.Application.Services;
using DeliveryAPI.Application.Models.Input;
using Microsoft.AspNetCore.Mvc;

namespace DeliveryAPI.Api.Controllers
{
    [ApiController]
    [Route("/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("start")]
        public async Task<IActionResult> Start([FromBody] AuthStartRequest request)
        {
            await _authService.StartAsync(request.PhoneNumber);

            return Ok("Code Sent");
        }

    }
}
