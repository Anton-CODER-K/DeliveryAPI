using DeliveryAPI.Application.Enums;
using DeliveryAPI.Application.Exeptions;
using System.Security.Claims;
using DeliveryAPI.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DeliveryAPI.Application.Models.Result;
using DeliveryAPI.Api.Contracts.Request;

namespace DeliveryAPI.Api.Controllers
{
    [ApiController]
    [Route("/courier/deliveries")]
    public class CourierController : ControllerBase
    {
        private readonly DeliveryService _deliveryService;
        private readonly CourierService _courierService;
        private readonly TrackingService _trackingService;

        public CourierController(DeliveryService deliveryService, CourierService courierService, TrackingService trackingService)
        {
            _deliveryService = deliveryService;
            _courierService = courierService;
            _trackingService = trackingService;
        }

        [Authorize(Roles = "Admin,Courier")]
        [HttpGet]
        [ProducesResponseType(typeof(List<DeliveryUserResult>), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        public async Task<ActionResult<List<DeliveryUserResult>>> GetDelivery([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] DeliveryStatus? deliveryStatus = null)
        {
            var delivery = await _deliveryService.GetDeliveriesByCourierAsync(page, pageSize, deliveryStatus);

            return Ok(delivery);
        }

        [Authorize(Roles = "Courier")]
        [HttpGet("my")]
        [ProducesResponseType(typeof(List<DeliveryUserResult>), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        public async Task<ActionResult<List<DeliveryUserResult>>> GetDeliveryMyActive([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] DeliveryStatus? deliveryStatus = null)
        {

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new UnauthorizedException("UserId claim missing");

            int userId = int.Parse(userIdClaim.Value);

            var delivery = await _deliveryService.GetDeliveriesByCourierAsync(page, pageSize, userId, deliveryStatus);

            return Ok(delivery);
        }


        [Authorize(Roles = "Courier")]
        [HttpPut("{id}/accepted")]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        public async Task<ActionResult<string>> DeliveryAcceptedByCourier([FromRoute] int id)
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
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        public async Task<ActionResult<string>> DeliveryPickedUpByCourier([FromRoute] int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new UnauthorizedException("UserId claim missing");

            int userId = int.Parse(userIdClaim.Value);

            await _deliveryService.PickedUpDeliveryByCourierAsync(id, userId);

            return Ok("PickedUp Delivery");
        }

        [Authorize(Roles = "Courier")]
        [HttpPut("{id}/confirmations")]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        public async Task<ActionResult<string>> DeliveryConfirmationsByCourier([FromRoute] int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new UnauthorizedException("UserId claim missing");

            int userId = int.Parse(userIdClaim.Value);

            await _deliveryService.ConfirmationsDeliveryByCourierAsync(id, userId);

            return Ok("PickedUp Delivery");
        }

        //[Authorize(Roles = "Courier")]
        //[HttpPost("location")]
        //public async Task UpdateLocation(LocationDto dto)
        //{
        //    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        //    if (userIdClaim == null)
        //        throw new UnauthorizedException("UserId claim missing");

        //    int courierId = int.Parse(userIdClaim.Value);

        //    await _courierService.UpdateLocation(courierId, dto);

        //    var orders = await _deliveryService.GetActiveOrders(courierId);

        //    foreach (var orderId in orders)
        //    {
        //        await _trackingService.SendLocation(orderId, dto);
        //    }
        //}
    }
}
