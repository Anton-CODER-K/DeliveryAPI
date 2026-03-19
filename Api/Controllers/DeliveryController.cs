using System.Security.Claims;
using DeliveryAPI.Api.Contracts.Request;
using DeliveryAPI.Application.Exeptions;
using DeliveryAPI.Application.Models.Input;
using DeliveryAPI.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeliveryAPI.Api.Controllers
{
    [ApiController]
    [Route("/deliveries")]
    public class DeliveryController : ControllerBase
    {
        private readonly DeliveryService _deliveryService;

        public DeliveryController(DeliveryService deliveryService)
        {
            _deliveryService = deliveryService;
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DeliveryCreateRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new UnauthorizedException("UserId claim missing");

            int userId = int.Parse(userIdClaim.Value);

            var result = await _deliveryService.CreateAsync(new DeliveryCreateInput
            {
                UserId = userId,
                AddressId = request.AddressId,
                PaymentMethodId = request.PaymentMethodId,
                Products = request.Products,
            });
            return Ok(new { id = result });
        }

        [Authorize]
        [HttpGet("my")]
        public async Task<IActionResult> GetDeliveriesByUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new UnauthorizedException("UserId claim missing");
            int userId = int.Parse(userIdClaim.Value);

            var result = await _deliveryService.GetDeliveriesByUserIdAsync(userId);
            return Ok(result);
        }

        [Authorize]
        [HttpPut("{id}/accepted")]
        public async Task<IActionResult> AcceptDeliveryByUser([FromRoute] int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new UnauthorizedException("UserId claim missing");
            int userId = int.Parse(userIdClaim.Value);
            await _deliveryService.AcceptDeliveryByUserAsync(id, userId);
            return Ok("Accepted Delivery");
        }

       
    }
}



