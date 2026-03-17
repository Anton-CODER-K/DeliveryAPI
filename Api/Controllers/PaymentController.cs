using System.Security.Claims;
using DeliveryAPI.Api.Contracts.Request;
using DeliveryAPI.Api.Contracts.Response;
using DeliveryAPI.Application.Exeptions;
using DeliveryAPI.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeliveryAPI.Api.Controllers
{
    [ApiController]
    [Route("payments")]
    public class PaymentController : ControllerBase
    {
        private readonly PaymentService _paymentService;

        public PaymentController(PaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [Authorize]
        [HttpPost("{deliveryId}")]
        public async Task<ActionResult<LiqPayCheckoutResponse>> CreatePayment(int deliveryId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new UnauthorizedException("UserId claim missing");

            int userId = int.Parse(userIdClaim.Value);

            var result = await _paymentService.CreatePayment(deliveryId, userId);

            return Ok(result);
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook([FromForm] LiqPayWebhookRequest request)
        {
            await _paymentService.HandleWebhook(request);

            return Ok();
        }
    }
}
