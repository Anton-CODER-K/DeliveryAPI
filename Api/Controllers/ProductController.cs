using DeliveryAPI.Api.Contracts.Request;
using DeliveryAPI.Api.Contracts.Response;
using DeliveryAPI.Api.Middleware;
using DeliveryAPI.Application.Exeptions;
using DeliveryAPI.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Security.Claims;

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


        // UNDONE: Треба буде додати в DTO фото і в сервісі обробку фото, але поки що так

        [Authorize(Roles = "Admin,RestaurantUser")]
        [HttpPost]
        [ProducesResponseType(typeof(int), 201)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        public async Task<ActionResult<int>> CreateProduct([FromBody] ProductCreateRequest request)
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

        // UNDONE: Додати видачу шляха фоток, але поки що так
        [Authorize]
        [HttpGet]
        [ProducesResponseType(typeof(List<ProductResponse>), 200)]
        public async Task<ActionResult<List<ProductResponse>>> GetProducts([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] int? categoryId = null, [FromQuery] int? restaurantId = null)
        {
            var result = await _productService.GetProductsAsync(page, pageSize, categoryId, restaurantId);

            return Ok(result);
        }

        [HttpPut("{productId}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        public async Task<ActionResult> UpdateProduct([FromRoute] int productId, [FromBody] ProductUpdateRequest request)
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
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        public async Task<ActionResult> DeleteProduct([FromQuery] int productId)
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

        // UNDONE: Треба буде додати в DTO фото і в сервісі обробку фото, але поки що так
        [Authorize(Roles = "Admin,RestaurantUser")]
        [HttpPut("{productId}/image")]
        public async Task<ActionResult> UploadProductImage([FromRoute] int productId, [FromForm] IFormFile image)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new UnauthorizedException("UserId claim missing");
            var roleClaim = User.FindFirst(ClaimTypes.Role);
            if (roleClaim == null)
                throw new UnauthorizedException("Role claim missing");
            int userId = int.Parse(userIdClaim.Value);
            string role = roleClaim.Value;


            var result = await _productService.UploadProductImageAsync(productId, image, userId, role);
            return Ok(result);
        }

        [Authorize(Roles = "Admin,RestaurantUser")]
        [HttpDelete("{productId}/image")]
        public async Task<ActionResult> DeleteProductImage([FromRoute] int productId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new UnauthorizedException("UserId claim missing");
            
            int userId = int.Parse(userIdClaim.Value);

            var result = await _productService.DeleteProductImageAsync(productId, userId);
            return Ok(result);
        }
    }
}
