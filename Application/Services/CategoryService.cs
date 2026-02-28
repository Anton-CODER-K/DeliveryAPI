using DeliveryAPI.Application.Models.Result;
using DeliveryAPI.Infrastructure.Database;
using DeliveryAPI.Infrastructure.Repositories;

namespace DeliveryAPI.Application.Services
{
    public class CategoryService
    {
        private readonly TransactionExecutor _tx;
        private readonly CategoryRepository _categoryRepository;
        private readonly ILogger<CategoryService> _logger;

        public CategoryService(TransactionExecutor tx, CategoryRepository categoryRepository, ILogger<CategoryService> logger)
        {
            _tx = tx;
            _categoryRepository = categoryRepository;
            _logger = logger;
        }

        public async Task<List<CategoryGet>> GetCategoriesAsync()
        {
            
            List<CategoryGet> categories = new List<CategoryGet>();

            await _tx.ExecuteAsync(async (conn, tx) =>
            {
                categories = await _categoryRepository.GetCategories(conn, tx);
            });

            return categories;
        }

        public async Task<int> CreateCategoryAsync(string name, int userId)
        {
            int result = 0;

            await _tx.ExecuteAsync(async (conn, tx) =>
            {
                result = await _categoryRepository.InsertCategory(conn, tx, name);
            });

            return result;

            _logger.LogInformation("User {userId} create delivery {deliveryId}", userId, result);
        }

        public async Task UpdateCategoryAsync(int categoryId, string name, int userId)
        {
            await _tx.ExecuteAsync(async (conn, tx) =>
            {
                await _categoryRepository.UpdateCategory(conn, tx, categoryId, name);
            });
        }
    }
}
