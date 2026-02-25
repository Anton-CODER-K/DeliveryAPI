using DeliveryAPI.Application.Enums;
using DeliveryAPI.Application.Exeptions;
using System.Security.Claims;
using DeliveryAPI.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeliveryAPI.Api.Controllers
{
    [ApiController]
    [Route("/courier/deliveries")]
    public class CourierController : ControllerBase
    {
        private readonly DeliveryService _deliveryService;

        public CourierController(DeliveryService deliveryService)
        {
            _deliveryService = deliveryService;
        }

        [Authorize(Roles = "Admin,Courier")]
        [HttpGet]
        public async Task<IActionResult> GetDelivery([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] DeliveryStatus deliveryStatus = DeliveryStatus.RestaurantConfirmed)
        {
            var delivery = await _deliveryService.GetDeliveriesByCourierAsync(page, pageSize, deliveryStatus);

            return Ok(delivery);
        }

        [Authorize(Roles = "Courier")]
        [HttpPut("{id}/accepted")]
        public async Task<IActionResult> DeliveryAcceptedByCourier([FromRoute] int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new UnauthorizedException("UserId claim missing");

            int userId = int.Parse(userIdClaim.Value);

            await _deliveryService.AcceptDeliveryByCourierAsync(id, userId);
 
            return Ok("Accepted Delivery");
        }

        [Authorize(Roles = "Courier")]
        [HttpPut("{id}/pickedup")]
        public async Task<IActionResult> DeliveryPickedUpByCourier([FromRoute] int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new UnauthorizedException("UserId claim missing");

            int userId = int.Parse(userIdClaim.Value);

            await _deliveryService.PickedUpDeliveryByCourierAsync(id, userId);

            return Ok("PickedUp Delivery");
        }
    }
}
