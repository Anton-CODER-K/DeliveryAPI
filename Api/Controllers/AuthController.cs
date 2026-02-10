using DeliveryAPI.Api.Contracts.Request;
using DeliveryAPI.Application.Services;
using DeliveryAPI.Application.Models.Input;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using System.Diagnostics.Contracts;
using DeliveryAPI.Api.Middleware;
using System.Security.Claims;

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

        [HttpPost("verify")]
        public async Task<IActionResult> Verify([FromBody] AuthVerifyRequest request)
        {
            var tokens = await _authService.VerifyAsync(request.PhoneNumber, request.Code);
            return Ok(tokens);
        }

        [HttpPost("set-password")]
        public async Task<IActionResult> SetPassword([FromBody] AuthSetPasswordRequest request)
        {
          
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers["User-Agent"].ToString();

            var authHeader = Request.Headers["Authorization"].ToString();

            if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
                return Unauthorized();

            var token = authHeader.Substring("Bearer ".Length).Trim();

            var result = await _authService.SetPasswordByTokenAsync(token, request.Password, request.Name, request.Birthday, ip, userAgent);

            return Ok(result);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] AuthRefreshRequest request)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers["User-Agent"].ToString();

            return Ok(await _authService.RefreshAsync(request.RefreshToken, ip, userAgent));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] AuthLoginRequest request)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers["User-Agent"].ToString();


            var tokens = await _authService.LoginAsync(request.PhoneNumber, request.Password, ip, userAgent);
            return Ok(tokens);
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new UnauthorizedException("UserId claim missing");

            var roleClaim = User.FindFirst(ClaimTypes.Role);
            if (roleClaim == null)
                throw new UnauthorizedException("Role claim missing");

            int userId = int.Parse(userIdClaim.Value);
            string role = roleClaim.Value;

            var result = await _authService.GetMeAsync(userId, role);
            return Ok(result);
        }

         



        //[Authorize]
        //[HttpGet("sessions")]
        //public async Task<IActionResult> GetSessions()
        //{
        //    var userId = int.Parse(User.FindFirst("userId")!.Value);

        //    var sessions = await _authService.GetSessionsUserIdAsync(userId);
        //    return Ok(sessions);
        //}

    }
}
