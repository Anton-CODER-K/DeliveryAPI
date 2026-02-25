using DeliveryAPI.Application.Models.Result;
using DeliveryAPI.Infrastructure.Database;
using DeliveryAPI.Infrastructure.Repositories;

namespace DeliveryAPI.Application.Services
{
    public class CategoryService
    {
        private readonly TransactionExecutor _tx;
        private readonly CategoryRepository _categoryRepository;

        public CategoryService(TransactionExecutor tx, CategoryRepository categoryRepository)
        {
            _tx = tx;
            _categoryRepository = categoryRepository;
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

        public async Task<int> CreateCategoryAsync(string name)
        {
            int result = 0;

            await _tx.ExecuteAsync(async (conn, tx) =>
            {
                result = await _categoryRepository.InsertCategory(conn, tx, name);
            });

            return result;
        }

        public async Task UpdateCategoryAsync(int categoryId, string name)
        {
            await _tx.ExecuteAsync(async (conn, tx) =>
            {
                await _categoryRepository.UpdateCategory(conn, tx, categoryId, name);
            });
        }
    }
}
