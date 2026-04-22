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
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(PaymentService paymentService, ILogger<PaymentController> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        [Authorize]
        [HttpPost("{deliveryId}")]
        [ProducesResponseType(typeof(LiqPayCheckoutResponse), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
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
        [ProducesResponseType(200)]
        public async Task<ActionResult> Webhook([FromForm] LiqPayWebhookRequest request)
        {
            _logger.LogInformation("Webhook data: {Data}", request.Data);
            _logger.LogInformation("Webhook signature: {Signature}", request.Signature);

            await _paymentService.HandleWebhook(request);
            return Ok();
           
        }
    }
}
