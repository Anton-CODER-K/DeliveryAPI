using System.Runtime.InteropServices;
using DeliveryAPI.Api.Contracts.Response;
using DeliveryAPI.Api.Middleware;
using DeliveryAPI.Infrastructure.Entity.ReadModel;
using DeliveryAPI.Infrastructure.Entity.Record;
using Npgsql;
using Npgsql.PostgresTypes;
using NpgsqlTypes;

namespace DeliveryAPI.Infrastructure.Repositories
{
    public class AddressRepository
    {
        public async Task<int> InsertAddress(NpgsqlConnection conn, NpgsqlTransaction tx, AddressCreateRepo record)
        {
            const string sql = """
                Insert Into addresses (user_id, title, latitude, longitude, house, apartment, entrance, floor, comment, is_default)
                Values (@userId, @title, @latitude, @longitude, @house, @apartment, @entrance, @floor, @comment, @isDefault)
                RETURNING address_id;
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.Add("@userId", NpgsqlDbType.Integer).Value = record.userId;
            cmd.Parameters.Add("@title", NpgsqlDbType.Varchar).Value = record.title;
            cmd.Parameters.Add("@latitude", NpgsqlDbType.Numeric).Value = record.latitude;
            cmd.Parameters.Add("@longitude", NpgsqlDbType.Numeric).Value = record.longitude;
            cmd.Parameters.Add("@house", NpgsqlDbType.Varchar).Value = record.house;
            cmd.Parameters.Add("@apartment", NpgsqlDbType.Varchar).Value = record.apartment ?? (object)DBNull.Value;
            cmd.Parameters.Add("@entrance", NpgsqlDbType.Varchar).Value = record.entrance ?? (object)DBNull.Value;
            cmd.Parameters.Add("@floor", NpgsqlDbType.Varchar).Value = record.floor ?? (object)DBNull.Value;
            cmd.Parameters.Add("@comment", NpgsqlDbType.Text).Value = record.comment ?? (object)DBNull.Value;
            cmd.Parameters.Add("@isDefault", NpgsqlDbType.Boolean).Value = record.is_default;

            var id = (int)await cmd.ExecuteScalarAsync();

            return id;
        }

        public async Task MarkAllAddressByUserNotDefault(NpgsqlConnection conn, NpgsqlTransaction tx, int userId)
        {
            const string sql = """
                UPDATE addresses
                SET is_default = false
                WHERE user_id = @userId
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.Add("@userId", NpgsqlDbType.Integer).Value = userId;

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<long> CheckLimitAddressByUserId(NpgsqlConnection conn, NpgsqlTransaction tx, int userId)
        {
            const string sql = """
                SELECT COUNT(*)
                FROM addresses
                WHERE user_id = @userId
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.Add("@userId", NpgsqlDbType.Integer).Value = userId;

            var addressCount = await cmd.ExecuteScalarAsync();

            return (long)addressCount;

        }

        public async Task<List<AddressUserIdResponse>> GetAddressByUserId(NpgsqlConnection conn, NpgsqlTransaction tx, int userId)
        {
            List<AddressUserIdResponse> addressUserIdResponses = new List<AddressUserIdResponse>();

            const string sql = """
                SELECT address_id, title, latitude, longitude, house, apartment, entrance, floor, comment, is_default
                FROM addresses
                WHERE user_id = @userId
                ORDER BY is_default DESC, address_id DESC
                
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.Add("@userId", NpgsqlDbType.Integer).Value = userId;

            await using var reader = await cmd.ExecuteReaderAsync();

            while(await reader.ReadAsync())
            {
                addressUserIdResponses.Add(new AddressUserIdResponse
                {
                    addressId = reader.GetInt32(0),
                    title = reader.GetString(1),
                    latitude = reader.GetDecimal(2),
                    longitude = reader.GetDecimal(3),
                    house = reader.GetString(4),
                    apartment = reader.IsDBNull(5) ? null : reader.GetString(5),
                    entrance = reader.IsDBNull(6) ? null : reader.GetString(6),
                    floor = reader.IsDBNull(7) ? null : reader.GetString(7),
                    comment = reader.IsDBNull(8) ? null : reader.GetString(8),
                    is_default = reader.GetBoolean(9),
                });
            }

            return addressUserIdResponses;
        }

        public async Task<int> SetDefaultAddress(NpgsqlConnection conn, NpgsqlTransaction tx, int userId, int addressId)
        {
            const string sql = """
                UPDATE addresses
                SET is_default = true
                WHERE user_id = @userId and address_id = @address_id              
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.Add("@userId", NpgsqlDbType.Integer).Value = userId;
            cmd.Parameters.Add("@address_id", NpgsqlDbType.Integer).Value = addressId;

            int affectedRows = await cmd.ExecuteNonQueryAsync();
            return affectedRows;

        }

        public async Task<int> DeleteAddress(NpgsqlConnection conn, NpgsqlTransaction tx, int userId, int addressId)
        {
            const string sql = """
                DELETE FROM addresses
                WHERE user_id = @userId and address_id = @address_id
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.Add("@userId", NpgsqlDbType.Integer).Value = userId;
            cmd.Parameters.Add("@address_id", NpgsqlDbType.Integer).Value = addressId;

            int affectedRows = await cmd.ExecuteNonQueryAsync();
            return affectedRows;


        }

        public async Task<AddressReadModel?> GetUserAddress(NpgsqlConnection conn, NpgsqlTransaction tx, int addressId, int userId)
        {
            const string sql = """
                Select latitude, longitude, house, apartment, entrance, floor, comment
                From addresses
                Where user_id = @userId and address_Id = @addressId
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.Add("@userId", NpgsqlDbType.Integer).Value = userId;
            cmd.Parameters.Add("@addressId", NpgsqlDbType.Integer).Value = addressId;

            await using var reader = await cmd.ExecuteReaderAsync();

            if (reader.Read())
            {
                return new AddressReadModel
                {
                    AddressId = addressId,
                    Latitude = reader.GetDecimal(0),
                    Longitude = reader.GetDecimal(1),
                    House = reader.GetString(2),
                    Apartment = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Entrance = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Floor = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Comment = reader.IsDBNull(6) ? null : reader.GetString(6),

                };
            }

            return null;
        }
    }
}
