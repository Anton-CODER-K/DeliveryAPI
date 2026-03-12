using DeliveryAPI.Application.Exeptions;
using System.Security.Claims;
using DeliveryAPI.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.HttpResults;
using DeliveryAPI.Application.Enums;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace DeliveryAPI.Api.Controllers
{
    [ApiController]
    [Route("/admin")]
    public class AdminController : Controller
    {
        private readonly DeliveryService _deliveryService;
        private readonly UserService _userService;

        public AdminController(DeliveryService deliveryService, UserService userService)
        {
            _deliveryService = deliveryService;
            _userService = userService;
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new UnauthorizedException("UserId claim missing");
            int userId = int.Parse(userIdClaim.Value);

            var result = await _userService.GetUsers(page, pageSize);

            return Ok(result);
        }



        [Authorize(Roles = "Admin")]
        [HttpPost("courier/{id}")]
        public async Task<IActionResult> CreateCourier([FromRoute] int id)
        {

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new UnauthorizedException("UserId claim missing");
            int userId = int.Parse(userIdClaim.Value);

            

            return Ok("Не робить пішов нахуй");
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("courier/{id}")]
        public async Task<IActionResult> DeleteCourier([FromRoute] int id)
        {

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new UnauthorizedException("UserId claim missing");
            int userId = int.Parse(userIdClaim.Value);




            return Ok("Не робить пішов нахуй");
        }
    }
}
