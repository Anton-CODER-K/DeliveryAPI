using DeliveryAPI.Api.Contracts.Request;
using DeliveryAPI.Application.Enums;
using DeliveryAPI.Application.Exeptions;
using DeliveryAPI.Application.Models.Result;
using DeliveryAPI.Application.Services;
using DeliveryAPI.Infrastructure.Entity.ReadModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

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
        [ProducesResponseType(typeof(List<DeliveryUserResult>), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        public async Task<ActionResult<List<DeliveryUserResult>>> GetDelivery([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] DeliveryStatus? status = DeliveryStatus.Created)
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
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        public async Task<ActionResult<string>> AcceptDelivery([FromRoute] int id)
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
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        public async Task<IActionResult> CookingDelivery([FromRoute] int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new UnauthorizedException("UserId claim missing");
            int userId = int.Parse(userIdClaim.Value);


            await _deliveryService.PreparingDeliveryByRestaurantAsync(userId, id);

            return Ok("Delivery Cooking");
        }

        [Authorize(Roles = "RestaurantUser")]
        [HttpPut("deliveries/{id}/ready")]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        public async Task<IActionResult> AlreadyCookingDelivery([FromRoute] int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new UnauthorizedException("UserId claim missing");
            int userId = int.Parse(userIdClaim.Value);


            await _deliveryService.ReadyDeliveryByRestaurantAsync(userId, id);

            return Ok("Delivery Ready To Picked");
        }



        [Authorize(Roles = "RestaurantUser")]
        [HttpPut("deliveries/{id}/cancel")]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        public async Task<ActionResult<string>> CancelDelivery([FromRoute] int id)
        {

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new UnauthorizedException("UserId claim missing");
            int userId = int.Parse(userIdClaim.Value);


            await _deliveryService.CancelDeliveryByRestaurantAsync(userId, id);

            return Ok("Delivery Canceled");
        }

        // Undone:  Фотки для категорій витягувати

        [Authorize]
        [HttpGet]
        [ProducesResponseType(typeof(List<RestaurantsReadListModels>), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        public async Task<ActionResult<List<RestaurantsReadListModels>>> GetReastaurant([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] int? categoryId = null)
        {
            var result = await _productService.GetRestaurantsAsync(page, pageSize, categoryId);

            return Ok(result);
        }

        [Authorize]
        [HttpPost("image")]
        [ProducesResponseType(typeof(int), 201)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<int>> UpdateRestaurantImage([FromForm] RestaurantUpdateImageRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new UnauthorizedException("UserId claim missing");

            var roleClaim = User.FindFirst(ClaimTypes.Role);
            if (roleClaim == null)
                throw new UnauthorizedException("Role claim missing");
            int userId = int.Parse(userIdClaim.Value);
            string role = roleClaim.Value;

            var result = await _productService.UpdateRestaurantImageAsync(request.RestaurantId, request.Image, userId, role);

            return Ok(result);

            // Undone:  Фотки для категорій добавити 

        }
    }
}