using DeliveryAPI.Application.Exeptions;
using System.Security.Claims;
using DeliveryAPI.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.HttpResults;
using DeliveryAPI.Application.Enums;

namespace DeliveryAPI.Api.Controllers
{
    [ApiController]
    [Route("/restaurant")]
    public class RestaurantController : Controller
    {
        private readonly DeliveryService _deliveryService;
        private readonly ProductService _productService;

        public RestaurantController(DeliveryService deliveryService, ProductService productService)
        {
            _deliveryService = deliveryService;
            _productService = productService;
        }

        [Authorize(Roles = "RestaurantUser")]
        [HttpGet("deliveries")]
        public async Task<IActionResult> GetDelivery([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] DeliveryStatus? status = DeliveryStatus.Created)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new UnauthorizedException("UserId claim missing");
            int userId = int.Parse(userIdClaim.Value);

            var delivery = await _deliveryService.GetDeliveriesByRestaurantUserAsync(page, pageSize, status, userId);

            return Ok(delivery);
        }

        [Authorize(Roles = "RestaurantUser")]
        [HttpPut("deliveries/{id}/confirm")]
        public async Task<IActionResult> AcceptDelivery([FromRoute] int id)
        {

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new UnauthorizedException("UserId claim missing");
            int userId = int.Parse(userIdClaim.Value);


            await _deliveryService.AcceptDeliveryByRestaurantAsync(userId, id);

            return Ok("Delivery Accepted");
        }

        [Authorize(Roles = "RestaurantUser")]
        [HttpPut("deliveries/{id}/cooking")]
        public async Task<IActionResult> CookingDelivery([FromRoute] int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new UnauthorizedException("UserId claim missing");
            int userId = int.Parse(userIdClaim.Value);


            await _deliveryService.CookingDeliveryByRestaurantAsync(userId, id);

            return Ok("Delivery Canceled");
        }


        [Authorize(Roles = "RestaurantUser")]
        [HttpPut("deliveries/{id}/cancel")]
        public async Task<IActionResult> CancelDelivery([FromRoute] int id)
        {

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new UnauthorizedException("UserId claim missing");
            int userId = int.Parse(userIdClaim.Value);


            await _deliveryService.CancelDeliveryByRestaurantAsync(userId, id);

            return Ok("Delivery Canceled");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetReastaurant([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery]int? categoryId = null)
        {
            var result = await _productService.GetRestaurantsAsync(page, pageSize, categoryId);

            return Ok(result);
        }

    }
}