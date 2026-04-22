using DeliveryAPI.Api.Contracts.Response;
using DeliveryAPI.Application.Enums;
using DeliveryAPI.Application.Models.Result;
using Npgsql;
using NpgsqlTypes;

namespace DeliveryAPI.Infrastructure.Repositories
{
    public class UserRepository
    {
        public async Task<List<Users>> GetUsers(NpgsqlConnection conn, NpgsqlTransaction tx, int offset, int pageSize, ConfirmationRole? role, string? query)
        {
            const string sql = """
                SELECT 
                    u.user_id,
                    u.name,
                    u.phone_number,
                    u.birth_date,
                    u.is_active,
                    u.created_at,
                    u.is_phone_verified,
                    s.last_seen_at,
                    r.name
                FROM users u
                LEFT JOIN LATERAL (
                    SELECT last_seen_at
                    FROM sessions
                    WHERE user_id = u.user_id
                    ORDER BY last_seen_at DESC
                    LIMIT 1
                ) s ON true
                JOIN roles r ON r.role_id = u.role_id
                WHERE
                (@role IS NULL OR u.role_id = @role)
                AND (
                    @query IS NULL
                    OR u.phone_number ILIKE '%' || @query || '%'
                    OR u.name ILIKE '%' || @query || '%'
                ) 
                ORDER BY u.created_at DESC
                LIMIT @limit OFFSET @offset;
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.Add("@limit", NpgsqlDbType.Integer).Value = pageSize;
            cmd.Parameters.Add("@offset", NpgsqlDbType.Integer).Value = offset;
            cmd.Parameters.Add("@query", NpgsqlDbType.Text).Value = string.IsNullOrWhiteSpace(query) ? DBNull.Value : query;
            cmd.Parameters.Add("@role", NpgsqlDbType.Integer).Value = role is null ? DBNull.Value : (int)role;

            var list = new List<Users>();

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new Users
                {
                    userId = reader.GetInt32(0),
                    name = reader.IsDBNull(1) ? null : reader.GetString(1),
                    phoneNumber = reader.GetString(2),
                    birthDay = reader.IsDBNull(3) ? null : DateOnly.FromDateTime(reader.GetDateTime(3)),
                    isActive = reader.GetBoolean(4),
                    createdAt = reader.GetDateTime(5),
                    isPhoneVerified = reader.IsDBNull(6) ? null : reader.GetBoolean(6),
                    lastSeenAt = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                    role = reader.GetString(8),
                });
            }

            return list;

        }

        //public async Task<List<UserAdminResult>> SearchUsers(
        //    NpgsqlConnection conn,
        //    NpgsqlTransaction tx,
        //    string? query,
        //    int offset,
        //    int limit)
        //{
        //    const string sql = """
        //        SELECT
        //            user_id,
        //            name,
        //            phone_number,
        //            created_at
        //        FROM users
        //        WHERE (@query IS NULL
        //            OR phone_number ILIKE '%' || @query || '%'
        //            OR name ILIKE '%' || @query || '%')
        //        ORDER BY created_at DESC
        //        LIMIT @limit OFFSET @offset
        //        """;

        //    await using var cmd = new NpgsqlCommand(sql, conn, tx);

        //    cmd.Parameters.AddWithValue("@query", (object?)query ?? DBNull.Value);
        //    cmd.Parameters.AddWithValue("@limit", limit);
        //    cmd.Parameters.AddWithValue("@offset", offset);

        //    var users = new List<UserAdminResult>();

        //    await using var reader = await cmd.ExecuteReaderAsync();

        //    while (await reader.ReadAsync())
        //    {
        //        users.Add(new UserAdminResult
        //        {
        //            UserId = reader.GetInt32(0),
        //            Name = reader.IsDBNull(1) ? null : reader.GetString(1),
        //            PhoneNumber = reader.GetString(2),
        //            CreatedAt = reader.GetDateTime(3)
        //        });
        //    }

        //    return users;
        //}
    }
}
