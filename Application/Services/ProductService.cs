
using DeliveryAPI.Api.Contracts.Response;
using DeliveryAPI.Api.Middleware;
using DeliveryAPI.Application.Exeptions;
using DeliveryAPI.Application.Interfaces;
using DeliveryAPI.Application.Models.Result;
using DeliveryAPI.Infrastructure.Database;
using DeliveryAPI.Infrastructure.Entity.ReadModel;
using DeliveryAPI.Infrastructure.Repositories;
using DeliveryAPI.Infrastructure.Storage;
using System.Collections.Generic;
using System.Data;
using System.Xml.Linq;

namespace DeliveryAPI.Application.Services
{
    public class ProductService
    {
        private readonly TransactionExecutor _tx;
        private readonly ProductRepository _productRepo;
        private readonly IImageStorage _imageStorage;
        private readonly ILogger<ProductService> _logger;

        public ProductService(TransactionExecutor tx, ProductRepository productRepo, ILogger<ProductService> logger, IImageStorage imageStorage)
        {
            _tx = tx;
            _productRepo = productRepo;
            _logger = logger;
            _imageStorage = imageStorage;
        }

        public async Task<int> CreateProductAsync(string name, decimal price, int weightGrams, int categoryId, string description, int restaurantId, IFormFile image, int userId, string role)
        {
            LocalImageStorage.IsImage(image);

            int result = 0;

            string? newImageFolder = null;


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


                try
                {
                    result = await _productRepo.InsertProduct(conn, tx, name, price, weightGrams, categoryId, description, restaurantId);

                    await using var stream = image.OpenReadStream();

                    var imageResult = await _imageStorage.SaveImageAsync(stream, "products");

                    newImageFolder = imageResult.Folder;

                    await _productRepo.UpdateProductPathFolder(conn, tx, result, newImageFolder);
                }
                catch
                {
                    if (!string.IsNullOrEmpty(newImageFolder))
                    {
                        await _imageStorage.DeleteImageAsync(newImageFolder);
                    }

                    throw;
                }
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

        public async Task<GetEntityToAddPhoto?> GetByIdToAddPhotoProductAsync(int productId)
        {
            GetEntityToAddPhoto? product = null;
            await _tx.ExecuteAsync(async (conn, tx) =>
            {
                product = await _productRepo.GetByIdToAddPhoto(conn, tx, productId);
            });
            return product;
        }


        public async Task<ImageVariants> UploadProductImageAsync(int productId, IFormFile image, int userId, string role)
        {
            LocalImageStorage.IsImage(image);

            var product = await GetByIdToAddPhotoProductAsync(productId);

            if (product == null)
                throw new BusinessException("PRODUCT NOT FOUND", "Product not found");

            ImageVariants result = null;

            string? newImageUrl = null;
            await _tx.ExecuteAsync(async (conn, tx) =>
            {
                try
                {
                    if (role != "Admin")
                    {
                        int? restaurantId = await _productRepo.CheckUserIdInRestaurant(conn, tx, userId);
                        if (restaurantId == null)
                        {
                            throw new ForbiddenException("You cannot manage this restaurant.");
                        }
                        if (!await _productRepo.CheckRestaurantIdInProduct(conn, tx, restaurantId.Value, productId))
                        {
                            throw new ForbiddenException("You cannot manage this product.");
                        }
                    }

                    await using var stream = image.OpenReadStream();
                    result = await _imageStorage.SaveImageAsync(stream, "products");

                    newImageUrl = result.Original;

                    var oldImagePath = product.ImageUrl;

                    product.ImageUrl = result.Folder;

                    int rows = await _productRepo.UpdateProductPathFolder(conn, tx, product.Id, product.ImageUrl);

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

                    _logger.LogError(ex, "Error uploading product image for productId {ProductId}", productId);
                    throw;
                }
            });

            return result;
        }

        public async Task<string> DeleteProductImageAsync(int productId, int userId)
        {
            //var product = await GetByIdAsync(productId);

            //if (product == null)
            //    throw new Exception("Product not found");

            //if (!await _permissionService.CanEditProduct(userId, product))
            //    throw new Exception("No access");

            //if (string.IsNullOrEmpty(product.ImageUrl))
            //    throw new Exception("Product has no image");

            //try
            //{


            //}
            //catch (Exception ex)
            //{
            //    // Логування помилки
            //}
            return "Not implemented";
        }

        public async Task<GetEntityToAddPhoto?> GetByIdToAddPhotoRestaurantAsync(int restaurantId)
        {
            GetEntityToAddPhoto? restaurant = null;
            await _tx.ExecuteAsync(async (conn, tx) =>
            {
                restaurant = await _productRepo.GetByIdToAddRestaurantPhoto(conn, tx, restaurantId);
            });
            return restaurant;
        }


        public async Task<ImageVariants> UpdateRestaurantImageAsync(int restaurantId, IFormFile image, int userId, string role)
        {
            LocalImageStorage.IsImage(image);

            var restaurant = await GetByIdToAddPhotoRestaurantAsync(restaurantId);

            if (restaurant == null)
                throw new BusinessException("RESTAURANT NOT FOUND", "Restaurant not found");

            ImageVariants result = null;

            string? newImageUrl = null;
            await _tx.ExecuteAsync(async (conn, tx) =>
            {
                try
                {
                    if (role != "Admin")
                    {
                        int? restaurantId = await _productRepo.CheckUserIdInRestaurant(conn, tx, userId);
                        if (restaurantId == null)
                        {
                            throw new ForbiddenException("You cannot manage this restaurant.");
                        }
                    }

                    await using var stream = image.OpenReadStream();
                    result = await _imageStorage.SaveImageAsync(stream, "restaurants");

                    newImageUrl = result.Original;

                    var oldImagePath = restaurant.ImageUrl;

                    restaurant.ImageUrl = result.Folder;

                    int rows = await _productRepo.UpdateRestaurantPathFolder(conn, tx, restaurant.Id, restaurant.ImageUrl);

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

                    _logger.LogError(ex, "Error uploading product image for restaurantId {restaurantId}", restaurantId);
                    throw;
                }
            });

            return result;
        }
    }
}
