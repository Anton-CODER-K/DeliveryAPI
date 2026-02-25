using DeliveryAPI.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
            var result = await _categoryService.CreateCategoryAsync(name);

            return CreatedAtAction(nameof(CreateCategory), new { id = result }, new { productId = result });
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{categoryId}")]
        public async Task<IActionResult> UpdateCategory([FromRoute] int categoryId, [FromBody] string name)
        {
            await _categoryService.UpdateCategoryAsync(categoryId, name);

            return NoContent();
        }
    }
}
