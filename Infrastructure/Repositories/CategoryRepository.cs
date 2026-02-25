using DeliveryAPI.Api.Contracts.Response;
using System.Collections.Generic;
using DeliveryAPI.Application.Models.Result;
using Npgsql;
using NpgsqlTypes;
using DeliveryAPI.Application.Exeptions;

namespace DeliveryAPI.Infrastructure.Repositories
{
    public class CategoryRepository
    {
        public async Task<List<CategoryGet>> GetCategories(NpgsqlConnection conn, NpgsqlTransaction tx)
        {
            const string sql = """
                Select category_id, name
                From product_categories
                Order By category_id Desc
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);
            await using var reader = await cmd.ExecuteReaderAsync();

            var list = new List<CategoryGet>();

            while (await reader.ReadAsync())
            {
                list.Add(new CategoryGet
                {
                    CategoryId = reader.GetInt32(0),
                    Name = reader.GetString(1)
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
    }
}
