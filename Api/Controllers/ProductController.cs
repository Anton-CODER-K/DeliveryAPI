using System.Security.Claims;
using DeliveryAPI.Api.Contracts.Request;
using DeliveryAPI.Api.Middleware;
using DeliveryAPI.Application.Exeptions;
using DeliveryAPI.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeliveryAPI.Api.Controllers
{
    [ApiController]
    [Route("/product")]
    public class ProductController : ControllerBase
    {
        private readonly ProductService _productService;

        public ProductController(ProductService productService)
        {
            _productService = productService;
        }

        [Authorize(Roles = "Admin,RestaurantUser")]
        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] ProductCreateRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new UnauthorizedException("UserId claim missing");

            var roleClaim = User.FindFirst(ClaimTypes.Role);
            if (roleClaim == null)
                throw new UnauthorizedException("Role claim missing");

            int userId = int.Parse(userIdClaim.Value);
            string role = roleClaim.Value;

            int result = await _productService.CreateProductAsync(request.Name, request.Price, request.WeightGrams, request.CategoryId, request.Description, request.RestaurantId, userId, role);

            return CreatedAtAction(nameof(CreateProduct), new { id = result }, new { productId = result });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetProducts([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] int? categoryId = null, [FromQuery] int? restaurantId = null)
        {
            var result = await _productService.GetProductsAsync(page, pageSize, categoryId, restaurantId);

            return Ok(result);
        }

        [Authorize]
        [HttpPut("{productId}")]
        public async Task<IActionResult> UpdateProduct([FromRoute] int productId, [FromBody] ProductUpdateRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new UnauthorizedException("UserId claim missing");
            var roleClaim = User.FindFirst(ClaimTypes.Role);
            if (roleClaim == null)
                throw new UnauthorizedException("Role claim missing");
            int userId = int.Parse(userIdClaim.Value);
            string role = roleClaim.Value;
            await _productService.UpdateProductAsync(productId, request.Name, request.Price, request.WeightGrams, request.CategoryId, request.Description, userId, role);
            return NoContent();
        }


        [Authorize(Roles = "Admin,RestaurantUser")]
        [HttpDelete("{productId}")]
        public async Task<IActionResult> DeleteProduct([FromQuery] int productId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new UnauthorizedException("UserId claim missing");
            var roleClaim = User.FindFirst(ClaimTypes.Role);
            if (roleClaim == null)
                throw new UnauthorizedException("Role claim missing");
            int userId = int.Parse(userIdClaim.Value);
            string role = roleClaim.Value;

            await _productService.DeleteProductAsync(productId, userId, role);
            return NoContent();


        }
    }
}
