using DeliveryAPI.Application.Exeptions;
using System.Security.Claims;
using DeliveryAPI.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration.UserSecrets;
using DeliveryAPI.Application.Models.Result;

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
       
        
        // Undone: Фотки для категорій витягувати
        [Authorize]
        [HttpGet]
        [ProducesResponseType(typeof(List<CategoryGet>), 200)]
        public async Task<ActionResult<List<CategoryGet>>> GetCategories()
        {
            var result = await _categoryService.GetCategoriesAsync();
            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ProducesResponseType(typeof(int), 201)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        public async Task<ActionResult<int>> CreateCategory([FromBody] string name)
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
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        public async Task<ActionResult> UpdateCategory([FromRoute] int categoryId, [FromBody] string name)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new UnauthorizedException("UserId claim missing");
            int userId = int.Parse(userIdClaim.Value);

            await _categoryService.UpdateCategoryAsync(categoryId, name, userId);

            return NoContent();
        }

        // Undone:  Фотки для категорій добавити 
    }
}
