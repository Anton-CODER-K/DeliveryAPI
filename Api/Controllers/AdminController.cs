using DeliveryAPI.Application.Exeptions;
using System.Security.Claims;
using DeliveryAPI.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.HttpResults;
using DeliveryAPI.Application.Enums;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using DeliveryAPI.Api.Contracts.Response;
using DeliveryAPI.Application.Models.Result;

namespace DeliveryAPI.Api.Controllers
{
    [ApiController]
    [Route("/admin")]
    public class AdminController : ControllerBase
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
        [ProducesResponseType(typeof(List<Users>), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        public async Task<ActionResult<List<Users>>> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] ConfirmationRole? role = null, [FromQuery] string? search = null)
        {

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new UnauthorizedException("UserId claim missing");
            int userId = int.Parse(userIdClaim.Value);

            var result = await _userService.GetUsers(page, pageSize, role, search);

            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("delivery")]
        [ProducesResponseType(typeof(List<DeliveryAdminResult>), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        public async Task<ActionResult<List<DeliveryAdminResult>>> GetDeliveries([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] DeliveryStatus? status = null)
        {
            var result = await _deliveryService.GetDeliveriesAsync(page, pageSize, status);

            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("delivery/{id}/address")]
        [ProducesResponseType(typeof(AddressDeliveryIdResponse), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        public async Task<ActionResult<AddressDeliveryIdResponse?>> GetDeliveriesAddress([FromRoute] int id)
        {
            var result = await _deliveryService.GetDeliveryAddressAsync(id);

            return Ok(result);
        }



        [Authorize(Roles = "Admin")]
        [HttpPost("courier/{id}")]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        public async Task<ActionResult<string>> CreateCourier([FromRoute] int id)
        {

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new UnauthorizedException("UserId claim missing");
            int userId = int.Parse(userIdClaim.Value);

            

            return Ok("Не робить пішов нахуй");
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("courier/{id}")]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        public async Task<ActionResult<string>> DeleteCourier([FromRoute] int id)
        {

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new UnauthorizedException("UserId claim missing");
            int userId = int.Parse(userIdClaim.Value);




            return Ok("Не робить пішов нахуй");
        }
    }
}
