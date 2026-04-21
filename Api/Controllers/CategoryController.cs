using DeliveryAPI.Api.Contracts.Request;
using DeliveryAPI.Application.Exeptions;
using DeliveryAPI.Application.Models.Result;
using DeliveryAPI.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration.UserSecrets;
using System.Security.Claims;

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
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<int>> CreateCategory([FromForm] CategoryCreateRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new UnauthorizedException("UserId claim missing");

            var roleClaim = User.FindFirst(ClaimTypes.Role);
            if (roleClaim == null)
                throw new UnauthorizedException("Role claim missing");

            int userId = int.Parse(userIdClaim.Value);
            string role = roleClaim.Value;

            var result = await _categoryService.CreateCategoryAsync(request.Name, request.Image, userId, role);

            return CreatedAtAction(nameof(CreateCategory),  new { categoryId = result });
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
        [Authorize(Roles = "Admin")]
        [HttpPut("image")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult> UploadCategoryImage([FromForm] UploadCategoryImageRequest request)
        {
            var roleClaim = User.FindFirst(ClaimTypes.Role);
            if (roleClaim == null)
                throw new UnauthorizedException("Role claim missing");
          
            string role = roleClaim.Value;


            var result = await _categoryService.UploadCategoryImageAsync(request.CategoryId, request.Image, role);
            return Ok(result);
        }
    }
}
