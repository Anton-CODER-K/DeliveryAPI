using DeliveryAPI.Application.Exeptions;
using System.Security.Claims;
using DeliveryAPI.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace DeliveryAPI.Api.Controllers
{
    [ApiController]
    [Route("/category")]
    public class CategoryController : ControllerBase
    {
        private readonly CategoryService _categoryService;

        public CategoryController(CategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var result = await _categoryService.GetCategoriesAsync();
            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] string name)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new UnauthorizedException("UserId claim missing");
            int userId = int.Parse(userIdClaim.Value);

            var result = await _categoryService.CreateCategoryAsync(name, userId);

            return CreatedAtAction(nameof(CreateCategory), new { id = result }, new { productId = result });
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{categoryId}")]
        public async Task<IActionResult> UpdateCategory([FromRoute] int categoryId, [FromBody] string name)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new UnauthorizedException("UserId claim missing");
            int userId = int.Parse(userIdClaim.Value);

            await _categoryService.UpdateCategoryAsync(categoryId, name, userId);

            return NoContent();
        }
    }
}
