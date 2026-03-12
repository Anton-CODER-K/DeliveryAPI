
using System.Collections.Generic;
using DeliveryAPI.Api.Contracts.Response;
using DeliveryAPI.Api.Middleware;
using DeliveryAPI.Application.Exeptions;
using DeliveryAPI.Infrastructure.Database;
using DeliveryAPI.Infrastructure.Repositories;

namespace DeliveryAPI.Application.Services
{
    public class ProductService
    {
        private readonly TransactionExecutor _tx;
        private readonly ProductRepository _productRepo;

        public ProductService(TransactionExecutor tx, ProductRepository productRepo)
        {
            _tx = tx;
            _productRepo = productRepo;
        }

        public async Task<int> CreateProductAsync(string name, decimal price, int weightGrams, int categoryId, string description, int restaurantId, int userId, string role)
        {
            int result = 0;

            await _tx.ExecuteAsync(async (conn, tx) =>
            {
                if (role != "Admin")
                { 
                    if (!await _productRepo.CheckUserIdInRestaurantId(conn, tx, userId, restaurantId))
                    {
                        throw new BusinessException("YOU CANNOT MANEGE THIS RESTAURANT","You cannot manage this restaurant.");
                    }
                }
                if (!await _productRepo.CheckRestaurantExists(conn, tx, restaurantId))
                {
                    throw new BusinessException("RESTAURANT NOT FOUND", "The specified restaurant does not exist.");
                }
                if (!await _productRepo.CheckCategoryExists(conn, tx, categoryId))
                {
                    throw new BusinessException("CATEGORY NOT FOUND", "The specified category does not exist.");
                }

                result = await _productRepo.InsertProduct(conn, tx, name, price, weightGrams, categoryId, description, restaurantId);
            });

            return result;
        }

        public async Task<List<ProductResponse>> GetProductsAsync(int page, int pageSize, int? categoryId, int? restaurantId)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 50) pageSize = 50;

            int offset = (page - 1) * pageSize;

            List<ProductResponse> products = new List<ProductResponse>();

            await _tx.ExecuteAsync(async (conn, tx) =>
            {
               products = await _productRepo.GetProducts(conn, tx, offset, pageSize, categoryId, restaurantId);
            });

            return products;
        }

        public async Task<List<string>> GetRestaurantsAsync(int page, int pageSize, int? categoryId)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 50) pageSize = 50;

            int offset = (page - 1) * pageSize;

            List<string> restaurants = new List<string>();

            await _tx.ExecuteAsync(async (conn, tx) =>
            {
                restaurants = await _productRepo.GetRestaurants(conn, tx, offset, pageSize, categoryId);
            });

            return restaurants;
        }
        public async Task DeleteProductAsync(int productId, int userId, string role)
        {
            await _tx.ExecuteAsync(async (conn, tx) =>
            {
                if (role != "Admin")
                {
                    int? restaurantId = await _productRepo.CheckUserIdInRestaurant(conn, tx, userId);
                    if (restaurantId == null)
                    {
                        throw new UnauthorizedException("UserId claim missing");
                    }
                    if (await _productRepo.CheckRestaurantIdInProduct(conn, tx, restaurantId.Value, productId))
                    {
                        await _productRepo.DeleteProduct(conn, tx, productId);
                    }
                    else
                    {
                        throw new BusinessException("YOU CANNOT MANEGE THIS RESTAURANT", "You cannot manage this restaurant.");
                    }
                }
                else
                {
                    await _productRepo.DeleteProduct(conn, tx, productId);
                }
            });
        }

        public async Task UpdateProductAsync(int productId, string? name, decimal? price, int? weightGrams, int? categoryId, string? description, int userId, string role)
        {
            if(categoryId == null && name == null && price == null && weightGrams == null && description == null)
            {
                throw new BusinessException("NO FIELDS TO UPDATE", "At least one field must be provided for update.");
            }
            await _tx.ExecuteAsync(async (conn, tx) =>
            {
                if (role != "Admin")
                {
                    int? restaurantId = await _productRepo.CheckUserIdInRestaurant(conn, tx, userId);

                    if(restaurantId == null)
                    {
                        throw new UnauthorizedException("UserId claim missing");
                    }

                    if (await _productRepo.CheckRestaurantIdInProduct(conn, tx, restaurantId.Value, productId))
                    {
                        await _productRepo.UpdateProduct(conn, tx, productId, name, price, weightGrams, categoryId, description);
                    }
                    else
                    {
                        throw new BusinessException("YOU CANNOT MANEGE THIS RESTAURANT", "You cannot manage this restaurant.");
                    }
                }
                else
                {
                    await _productRepo.UpdateProduct(conn, tx, productId, name, price, weightGrams, categoryId, description);
                }
               

               
            });
        }

       
    }
}
