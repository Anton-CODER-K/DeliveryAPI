using DeliveryAPI.Api.Contracts.Response;
using DeliveryAPI.Application.Exeptions;
using DeliveryAPI.Application.Interfaces;
using DeliveryAPI.Application.Models.Result;
using DeliveryAPI.Infrastructure.Database;
using DeliveryAPI.Infrastructure.Entity.ReadModel;
using DeliveryAPI.Infrastructure.Repositories;
using DeliveryAPI.Infrastructure.Storage;
using Npgsql;
using System.Data;

namespace DeliveryAPI.Application.Services
{
    public class CategoryService
    {
        private readonly TransactionExecutor _tx;
        private readonly CategoryRepository _categoryRepo;
        private readonly IImageStorage _imageStorage;
        private readonly ILogger<CategoryService> _logger;

        public CategoryService(TransactionExecutor tx, CategoryRepository categoryRepository, ILogger<CategoryService> logger, IImageStorage imageStorage)
        {
            _tx = tx;
            _categoryRepo = categoryRepository;
            _imageStorage = imageStorage;
            _logger = logger;
        }

      
        public async Task<List<CategoryGet>> GetCategoriesAsync()
        {
            
            List<CategoryGet> categories = new List<CategoryGet>();

            await _tx.ExecuteAsync(async (conn, tx) =>
            {
                categories = await _categoryRepo.GetCategories(conn, tx);
            });

            return categories;
        }

        public async Task<int> CreateCategoryAsync(string name, IFormFile image, int userId, string role)
        {
            LocalImageStorage.IsImage(image);

            int result = 0;

            string? newImageFolder = null;



            await _tx.ExecuteAsync(async (conn, tx) =>
            {
                if (role != "Admin")
                {
                    throw new ForbiddenException("You cannot manage this.");
                }

                try
                {
                    result = await _categoryRepo.InsertCategory(conn, tx, name);

                    await using var stream = image.OpenReadStream();

                    var imageResult = await _imageStorage.SaveImageAsync(stream, "categories");

                    newImageFolder = imageResult.Folder;

                    await _categoryRepo.UpdateCategoryPathFolder(conn, tx, result, newImageFolder);

                }
                catch (PostgresException ex) when (ex.SqlState == "23505")
                {
                    throw new BusinessException("CATEGORY EXISTS", "Category already exists");
                }
                catch (Exception ex)
                {
                    if (!string.IsNullOrEmpty(newImageFolder))
                    {
                        await _imageStorage.DeleteImageAsync(newImageFolder);
                    }

                    _logger.LogError(ex, "Error uploading product image for category {categoryId}", result);
                    throw;
                }

            });

            _logger.LogInformation("User {userId} create category {categoryId}", userId, result);

            return result;

        }


        public async Task UpdateCategoryAsync(int categoryId, string name, int userId)
        {
            await _tx.ExecuteAsync(async (conn, tx) =>
            {
                await _categoryRepo.UpdateCategory(conn, tx, categoryId, name);
            });
        }

        public async Task<GetEntityToAddPhoto?> GetByIdToAddPhotoAsync(int categoryId)
        {
            GetEntityToAddPhoto? category = null;
            await _tx.ExecuteAsync(async (conn, tx) =>
            {
                category = await _categoryRepo.GetByIdToAddPhoto(conn, tx, categoryId);
            });
            return category;
        }

        public async Task<ImageVariants> UploadCategoryImageAsync(int categoryId, IFormFile image, string role)
        {
            LocalImageStorage.IsImage(image);

            var category = await GetByIdToAddPhotoAsync(categoryId);

            if (category == null)
                throw new BusinessException("PRODUCT NOT FOUND", "Product not found");

            ImageVariants result = null;

            string? newImageUrl = null;

            await _tx.ExecuteAsync(async (conn, tx) =>
            {
                try
                {
                    if (role != "Admin")
                    {
                        throw new ForbiddenException("You cannot manage this.");
                    }

                    await using var stream = image.OpenReadStream();
                    result = await _imageStorage.SaveImageAsync(stream, "categories");

                    newImageUrl = result.Original;

                    var oldImagePath = category.ImageUrl;

                    category.ImageUrl = result.Folder;

                    int rows = await _categoryRepo.UpdateCategoryPathFolder(conn, tx, category.Id, category.ImageUrl);

                    if (!string.IsNullOrEmpty(oldImagePath))
                    {
                        await _imageStorage.DeleteImageAsync(oldImagePath);
                    }

                    return result;

                }
                catch (Exception ex)
                {
                    if (!string.IsNullOrEmpty(newImageUrl))
                    {
                        await _imageStorage.DeleteImageAsync(newImageUrl);
                    }

                    _logger.LogError(ex, "Error uploading product image for productId {ProductId}", categoryId);
                    throw;
                }
            });

            return result;
        }
    }
}
