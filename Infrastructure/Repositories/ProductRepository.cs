using DeliveryAPI.Api.Contracts.Request;
using DeliveryAPI.Api.Contracts.Response;
using DeliveryAPI.Api.Middleware;
using DeliveryAPI.Application.Exeptions;
using DeliveryAPI.Common;
using DeliveryAPI.Infrastructure.Entity.ReadModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Npgsql;
using NpgsqlTypes;

namespace DeliveryAPI.Infrastructure.Repositories
{
    public class ProductRepository
    {
        public async Task<bool> CheckCategoryExists(NpgsqlConnection conn, NpgsqlTransaction tx, int categoryId)
        {
            const string sql = """
                Select 1
                From product_categories
                Where category_id = @categoryId
                Limit 1
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);
            cmd.Parameters.Add("@categoryId", NpgsqlDbType.Integer).Value = categoryId;

            var result = await cmd.ExecuteScalarAsync();

            return result != null;
        }

        internal async Task<bool> CheckRestaurantExists(NpgsqlConnection conn, NpgsqlTransaction tx, int restaurantId)
        {
            const string sql = """
                Select 1
                From restaurants
                Where restaurant_id = @restaurantId
                Limit 1
                """;

            await using var cmd = new NpgsqlCommand( sql, conn, tx);
            cmd.Parameters.Add("@restaurantId", NpgsqlDbType.Integer).Value = restaurantId;

            var result = await cmd.ExecuteScalarAsync();

            return result != null;

        }

        public async Task<bool> CheckUserIdInRestaurantId(NpgsqlConnection conn, NpgsqlTransaction tx, int userId, int restaurantId)
        {
            const string sql = """
                SELECT 1
                FROM restaurant_users
                WHERE user_id = @userId
                AND restaurant_id = @restaurantId
                LIMIT 1;
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);
            cmd.Parameters.Add("@userId", NpgsqlDbType.Integer).Value = userId;
            cmd.Parameters.Add("@restaurantId", NpgsqlDbType.Integer).Value = restaurantId;

            var result = await cmd.ExecuteScalarAsync();

            return result != null;
        }

        public async Task<int> InsertProduct(NpgsqlConnection conn, NpgsqlTransaction tx, string name, decimal price, int weightGrams, int categoryId, string description, int restaurantId)
        {
            const string sql = """
                Insert Into product (name, price, weight_grams, category_id, description, restaurant_id)
                Values (@name, @price, @weightGrams, @categoryId, @description, @restaurantId)
                returning product_id
                """;

            await using var cmd = new NpgsqlCommand( sql, conn, tx);
            cmd.Parameters.Add("@name", NpgsqlDbType.Varchar).Value = name;
            cmd.Parameters.Add("@price", NpgsqlDbType.Numeric).Value = price;
            cmd.Parameters.Add("@weightGrams", NpgsqlDbType.Integer).Value = weightGrams;
            cmd.Parameters.Add("@categoryId", NpgsqlDbType.Integer).Value = categoryId;
            cmd.Parameters.Add("@description", NpgsqlDbType.Text).Value = description;
            cmd.Parameters.Add("@restaurantId", NpgsqlDbType.Integer).Value = restaurantId;

            var id = (int)await cmd.ExecuteScalarAsync();

            return id;
           
        }

        public async Task<List<ProductResponse>> GetProducts(NpgsqlConnection conn, NpgsqlTransaction tx, int offset, int pageSize, int? categoryId, int? restaurantId)  
        {
            const string sql = """
                SELECT p.product_id, p.name, p.price, p.weight_grams, p.category_id, p.restaurant_id, p.description, i.folder
                FROM product p
                Left Join images i On i.image_id = p.image_id 
                WHERE (@categoryId IS NULL OR p.category_id = @categoryId)
                AND (@restaurantId IS NULL OR p.restaurant_id = @restaurantId)
                And p.is_active = true
                ORDER BY p.product_id DESC
                LIMIT @limit OFFSET @offset;
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.Add("@limit", NpgsqlDbType.Integer).Value = pageSize;
            cmd.Parameters.Add("@offset", NpgsqlDbType.Integer).Value = offset;
            cmd.Parameters.Add("@categoryId", NpgsqlDbType.Integer).Value = (object?)categoryId ?? DBNull.Value;
            cmd.Parameters.Add("@restaurantId", NpgsqlDbType.Integer).Value = (object?)restaurantId ?? DBNull.Value;

            var list = new List<ProductResponse>();

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new ProductResponse
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Price = reader.GetDecimal(2),
                    WeightGrams = reader.GetInt32(3),
                    CategoryId = reader.GetInt32(4),
                    RestaurantId = reader.GetInt32(5),
                    Description = reader.IsDBNull(6) ? null : reader.GetString(6),
                    URLBase = reader.IsDBNull(7) ? null : ($"{AppConfigURLBase.BaseUrl}" + "/images/" + reader.GetString(7) + "/thumb.jpg"),
                });
            }

            return list;
        }

        public async Task<int?> CheckUserIdInRestaurant(NpgsqlConnection conn, NpgsqlTransaction tx, int userId)
        {
            const string sql = """
                SELECT restaurant_id
                FROM restaurant_users
                WHERE user_id = @userId
                LIMIT 1;
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);
            cmd.Parameters.Add("@userId", NpgsqlDbType.Integer).Value = userId;


            var result = await cmd.ExecuteScalarAsync();

            if (result == null)
                return null; 

            int value = (int)result;

            return value;
        }

        public async Task<bool> CheckRestaurantIdInProduct(NpgsqlConnection conn, NpgsqlTransaction tx, int restaurantId, int productId)
        {
            const string sql = """
                Select 1
                From product
                Where restaurant_id = @restaurantId
                And product_id = @productId
                Limit 1
                """;
            
            await using var cmd = new NpgsqlCommand(sql, conn, tx);
            cmd.Parameters.Add("@restaurantId", NpgsqlDbType.Integer).Value = restaurantId;
            cmd.Parameters.Add("@productId", NpgsqlDbType.Integer).Value = productId;

            var result = await cmd.ExecuteScalarAsync();

            return result != null;
        }

        public async Task DeleteProduct(NpgsqlConnection conn, NpgsqlTransaction tx, int productId)
        {
            const string sql = """
                Update product
                Set is_active = false
                Where product_id = @productId
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);
            cmd.Parameters.Add("@productId", NpgsqlDbType.Integer).Value = productId;

            var result = await cmd.ExecuteScalarAsync();

            if (result == null)
                throw new BusinessException("PRODUCT_NOT_FOUND", "Product not found for user.");
        }

        public async Task UpdateProduct(NpgsqlConnection conn, NpgsqlTransaction tx, int productId, string? name, decimal? price, int? weightGrams, int? categoryId, string? description)
        {
            const string sql = """
                UPDATE product
                SET 
                    name = COALESCE(@name, name),
                    price = COALESCE(@price, price),
                    weight_grams = COALESCE(@weightGrams, weight_grams),
                    category_id = COALESCE(@categoryId, category_id),
                    description = COALESCE(@description, description)
                WHERE product_id = @productId
                AND is_active = true;
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.Add("@name", NpgsqlDbType.Varchar).Value = (object?)name ?? DBNull.Value;
            cmd.Parameters.Add("@price", NpgsqlDbType.Numeric).Value = (object?)price ?? DBNull.Value;
            cmd.Parameters.Add("@weightGrams", NpgsqlDbType.Integer).Value = (object?)weightGrams ?? DBNull.Value;
            cmd.Parameters.Add("@categoryId", NpgsqlDbType.Integer).Value = (object?)categoryId ?? DBNull.Value;
            cmd.Parameters.Add("@description", NpgsqlDbType.Text).Value = (object?)description ?? DBNull.Value;
            cmd.Parameters.Add("@productId", NpgsqlDbType.Integer).Value = productId;

            int rows = await cmd.ExecuteNonQueryAsync();

            if (rows == 0)
                throw new BusinessException("PRODUCT_NOT_FOUND", "Product not found.");


        }

        public async Task<bool> CheckProductExists(NpgsqlConnection conn, NpgsqlTransaction tx, int productId)
        {
            const string sql = """
                Select 1
                From product
                Where product_id = @productId
                Limit 1
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);
            cmd.Parameters.Add("@productId", NpgsqlDbType.Integer).Value = productId;

            var result = await cmd.ExecuteScalarAsync();

            return result != null;
        }

        public async Task<List<ProductOrderReadModel>> GetProductsByIds(NpgsqlConnection conn, NpgsqlTransaction tx, List<CreateDeliveryProduct> products)
        {
            var productIds = products
               .Select(p => p.ProductId)
               .ToArray();

            const string sql = """
                SELECT product_id, name, price, weight_grams, restaurant_id
                FROM product
                WHERE product_id = ANY(@ids)
                AND is_active = true;
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.Add("@ids", NpgsqlDbType.Array | NpgsqlDbType.Integer)
                .Value = productIds;

            var result = new List<ProductOrderReadModel>();

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                int productId = reader.GetInt32(0);

                var quantity = products
                    .First(p => p.ProductId == productId)
                    .Quantity;

                result.Add(new ProductOrderReadModel
                {
                    ProductId = productId,
                    Name = reader.GetString(1),
                    Price = reader.GetDecimal(2),
                    WeightGrams = reader.GetInt32(3),
                    RestaurantId = reader.GetInt32(4),
                    Quantity = quantity
                });
            }

            return result;
        }

        public async Task<List<RestaurantsReadListModels>> GetRestaurants(
            NpgsqlConnection conn,
            NpgsqlTransaction tx,
            int offset,
            int pageSize,
            int? categoryId)
        {
            const string sql = """
                SELECT r.name, r.restaurant_id, i.folder, r.description
                FROM restaurants r
                LEFT JOIN images i ON i.image_id = r.image_id
                WHERE
                    @categoryId IS NULL
                    OR EXISTS (
                        SELECT 1
                        FROM product p
                        WHERE p.restaurant_id = r.restaurant_id
                        AND p.category_id = @categoryId
                    )
                ORDER BY r.restaurant_id DESC
                LIMIT @limit OFFSET @offset;
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.Add("@limit", NpgsqlTypes.NpgsqlDbType.Integer).Value = pageSize;
            cmd.Parameters.Add("@offset", NpgsqlTypes.NpgsqlDbType.Integer).Value = offset;
            cmd.Parameters.Add("@categoryId", NpgsqlTypes.NpgsqlDbType.Integer)
                .Value = (object?)categoryId ?? DBNull.Value;

            var list = new List<RestaurantsReadListModels>();

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new RestaurantsReadListModels
                {
                    Name = reader.GetString(0),
                    RestaurantId = reader.GetInt32(1),
                    URLBase = reader.IsDBNull(2) ? null : ($"{AppConfigURLBase.BaseUrl}" + "/images/" + reader.GetString(2) + "/medium.jpg"),
                    Description = reader.IsDBNull(3) ? null : reader.GetString(3)
                });
            }

            return list;
        }

        public async Task<GetEntityToAddPhoto?> GetByIdToAddPhoto(NpgsqlConnection conn, NpgsqlTransaction tx, int productId)
        {
            const string sql = """
                SELECT product_id, i.folder
                FROM product p
                Left Join images i on i.image_id = p.image_id
                WHERE product_id = @productId
                LIMIT 1;
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);
            cmd.Parameters.Add("@productId", NpgsqlDbType.Integer).Value = productId;

            await using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new GetEntityToAddPhoto
                {
                    Id = reader.GetInt32(0),
                    ImageUrl = reader.IsDBNull(1) ? null : reader.GetString(1)
                };
            }

            return null;
        }

        public async Task<int> UpdateProductPathFolder(NpgsqlConnection conn, NpgsqlTransaction tx, int productId, string imageUrl)
        {
            const string sql = """
                WITH new_image AS (
                    INSERT INTO images (folder)
                    SELECT @folder
                    FROM product p
                    WHERE p.product_id = @productId
                      AND p.image_id IS NULL
                    RETURNING image_id
                ),
                updated_product AS (
                    UPDATE product p
                    SET image_id = COALESCE(
                        (SELECT image_id FROM new_image),
                        p.image_id
                    )
                    WHERE p.product_id = @productId
                    RETURNING p.image_id
                )
                UPDATE images i
                SET folder = @folder
                WHERE i.image_id = (SELECT image_id FROM updated_product);
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);
            cmd.Parameters.Add("@productId", NpgsqlDbType.Integer).Value = productId;
            cmd.Parameters.Add("@folder", NpgsqlDbType.Varchar).Value = imageUrl;

            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<GetEntityToAddPhoto?> GetByIdToAddRestaurantPhoto(NpgsqlConnection conn, NpgsqlTransaction tx, int restaurantId)
        {
            const string sql = """
                SELECT restaurant_id, i.folder
                FROM restaurants r
                Left Join images i on i.image_id = r.image_id
                WHERE restaurant_id = @restaurantId
                LIMIT 1;
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);
            cmd.Parameters.Add("@restaurantId", NpgsqlDbType.Integer).Value = restaurantId;

            await using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new GetEntityToAddPhoto
                {
                    Id = reader.GetInt32(0),
                    ImageUrl = reader.IsDBNull(1) ? null : reader.GetString(1)
                };
            }

            return null;
        }

        public async Task<int> UpdateRestaurantPathFolder(NpgsqlConnection conn, NpgsqlTransaction tx, int id, string imageUrl)
        {
            const string sql = """
                WITH new_image AS (
                    INSERT INTO images (folder)
                    SELECT @folder
                    FROM restaurants r
                    WHERE r.restaurant_id = @restaurantId
                      AND r.image_id IS NULL
                    RETURNING image_id
                ),
                updated_restaurant AS (
                    UPDATE restaurants r
                    SET image_id = COALESCE(
                        (SELECT image_id FROM new_image),
                        r.image_id
                    )
                    WHERE r.restaurant_id = @restaurantId
                    RETURNING r.image_id
                )
                UPDATE images i
                SET folder = @folder
                WHERE i.image_id = (SELECT image_id FROM updated_restaurant);
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);
            cmd.Parameters.Add("@restaurantId", NpgsqlDbType.Integer).Value = id;
            cmd.Parameters.Add("@folder", NpgsqlDbType.Varchar).Value = imageUrl;

            return await cmd.ExecuteNonQueryAsync();
        }
    }
}
