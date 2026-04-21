using DeliveryAPI.Api.Contracts.Response;
using DeliveryAPI.Application.Exeptions;
using DeliveryAPI.Application.Models.Result;
using DeliveryAPI.Common;
using DeliveryAPI.Infrastructure.Entity.ReadModel;
using Npgsql;
using NpgsqlTypes;
using System.Collections.Generic;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DeliveryAPI.Infrastructure.Repositories
{
    public class CategoryRepository
    {
        public async Task<List<CategoryGet>> GetCategories(NpgsqlConnection conn, NpgsqlTransaction tx)
        {
            const string sql = """
                Select pc.category_id, pc.name, i.folder
                From product_categories pc
                Left Join images i on i.image_id = pc.image_id
                Order By pc.category_id Desc
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);
            await using var reader = await cmd.ExecuteReaderAsync();

            var list = new List<CategoryGet>();

            while (await reader.ReadAsync())
            {
                list.Add(new CategoryGet
                {
                    CategoryId = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    URLBase = reader.IsDBNull(2) ? null : ($"{AppConfigURLBase.BaseUrl}" + "/images/" + reader.GetString(2) + "/thumb.jpg")
                });
            }

            return list;
        }

        public async Task<int> InsertCategory(NpgsqlConnection conn, NpgsqlTransaction tx, string name)
        {
            const string sql = """
                Insert Into product_categories (name)
                Values (@name)
                returning category_id
                """;
            await using var cmd = new NpgsqlCommand(sql, conn, tx);
            cmd.Parameters.Add("@name", NpgsqlDbType.Varchar).Value = name;

            var id = (int)await cmd.ExecuteScalarAsync();

            return id;
        }

        public async Task UpdateCategory(NpgsqlConnection conn, NpgsqlTransaction tx, int categoryId, string name)
        {
            const string sql = """
                Update product_categories
                Set name = @name
                Where category_id = @categoryId
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);
            cmd.Parameters.Add("@name", NpgsqlDbType.Varchar).Value = name;
            cmd.Parameters.Add("@categoryId", NpgsqlDbType.Integer).Value = categoryId;

            int rows = await cmd.ExecuteNonQueryAsync();

            if (rows == 0)
                throw new BusinessException("PRODUCT_NOT_FOUND", "Product not found.");

        }

        public async Task<GetEntityToAddPhoto?> GetByIdToAddPhoto(NpgsqlConnection conn, NpgsqlTransaction tx, int categoryId)
        {
            const string sql = """
                SELECT category_id, i.folder
                FROM product_categories p
                Left Join images i on i.image_id = p.image_id
                WHERE category_id = @categoryId
                LIMIT 1;
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);
            cmd.Parameters.Add("@categoryId", NpgsqlDbType.Integer).Value = categoryId;

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

        internal async Task<int> UpdateCategoryPathFolder(NpgsqlConnection conn, NpgsqlTransaction tx, int categoryId, string imageUrl)
        {
            const string sql = """
                WITH new_image AS (
                    INSERT INTO images (folder)
                    SELECT @folder
                    FROM product_categories pg
                    WHERE pg.category_id = @categoryId
                      AND pg.image_id IS NULL
                    RETURNING image_id
                ),
                updated_category AS (
                    UPDATE product_categories pg
                    SET image_id = COALESCE(
                        (SELECT image_id FROM new_image),
                        pg.image_id
                    )
                    WHERE pg.category_id = @categoryId
                    RETURNING pg.image_id
                )
                UPDATE images i
                SET folder = @folder
                WHERE i.image_id = (SELECT image_id FROM updated_category);
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);
            cmd.Parameters.Add("@categoryId", NpgsqlDbType.Integer).Value = categoryId;
            cmd.Parameters.Add("@folder", NpgsqlDbType.Varchar).Value = imageUrl;

            return await cmd.ExecuteNonQueryAsync();
        }
    }
}
