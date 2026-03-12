using DeliveryAPI.Api.Contracts.Response;
using DeliveryAPI.Application.Models.Result;
using Npgsql;
using NpgsqlTypes;

namespace DeliveryAPI.Infrastructure.Repositories
{
    public class UserRepository
    {
        public async Task<List<Users>> GetUsers(NpgsqlConnection conn, NpgsqlTransaction tx, int offset, int pageSize)
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
                ORDER BY u.user_id DESC
                LIMIT @limit OFFSET @offset;
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.Add("@limit", NpgsqlDbType.Integer).Value = pageSize;
            cmd.Parameters.Add("@offset", NpgsqlDbType.Integer).Value = offset;

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
    }
}
