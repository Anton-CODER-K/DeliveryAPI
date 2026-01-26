using System.Numerics;
using System.Xml.Linq;
using DeliveryAPI.Api.Middleware;
using DeliveryAPI.Application.Models.Result;
using DeliveryAPI.Infrastructure.Entity.Record;
using Microsoft.AspNetCore.Identity;
using Npgsql;
using NpgsqlTypes;

namespace DeliveryAPI.Infrastructure.Repositories
{
    public class AuthRepository
    {
        public async Task<int> UserRegister(NpgsqlConnection conn, NpgsqlTransaction tx, string Phone)
        {
            const string sql = """

                Insert Into Users(phone_number)
                Values(@phone_number)
                returning user_id
                """
            ;

            await using var cmd = new NpgsqlCommand(sql,conn,tx);

            cmd.Parameters.Add(
                "@phone_number",
                NpgsqlDbType.Varchar
            ).Value = Phone;



            int rows = (int)await cmd.ExecuteScalarAsync();

            if (rows == null)
            {
                throw new BusinessException("INSERT_ERROR", "Internal server error ");
            }

            return rows;
        }

        public async Task<int?> GetUserIdByPhone(NpgsqlConnection conn, NpgsqlTransaction tx, string phoneNumber)
        {
            const string sql = """
                SELECT user_id
                FROM users
                WHERE phone_number = @phone;
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);
            cmd.Parameters.Add("@phone", NpgsqlDbType.Varchar).Value = phoneNumber;


            var result = await cmd.ExecuteScalarAsync();
            return result == null ? null : (int)result;

        }

        public async Task InsertLoginCode(NpgsqlConnection conn, NpgsqlTransaction tx, string phone, string hashCode, int userId)
        {
            const string sql = """
                INSERT INTO phone_verification_codes
                    (user_id, phone_number, code_hash, expires_at, attempts_left, last_sent_at)
                VALUES
                    (@userId, @phone, @codeHash, now() + interval '3 minutes', 3, now());
                
                """;

            await using var cmd = new NpgsqlCommand( sql, conn, tx);

            cmd.Parameters.Add("@userId", NpgsqlDbType.Bigint).Value = userId;
            cmd.Parameters.Add("@phone", NpgsqlDbType.Varchar).Value = phone;
            cmd.Parameters.Add("@codeHash", NpgsqlDbType.Text).Value = hashCode;

            await cmd.ExecuteNonQueryAsync();


        }

        public async Task<GetActiveCodeByPhoneResult?> GetActiveCodeByPhone(NpgsqlConnection conn, NpgsqlTransaction tx, string phone)
        {
            const string sql = """
                select 
                    phone_verification_codes_id,
                    last_sent_at,
                    sent_day,
                    sent_count_today
                From phone_verification_codes
                Where phone_number = @phone
                    AND used = false
                    AND expires_at > now()
                ORDER BY last_sent_at DESC
                    LIMIT 1;
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.Add("@phone", NpgsqlDbType.Varchar).Value = phone;

            await using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new GetActiveCodeByPhoneResult
                {
                    phoneVerificationCodesId = reader.GetInt32(0),
                    LastSentAt = reader.GetDateTime(1),
                    SentDay = DateOnly.FromDateTime(reader.GetDateTime(2)),
                    SentCountToday = reader.GetInt32(3),
                };
            }

            return null;

        }

        public async Task UpdateLoginCode(NpgsqlConnection conn, NpgsqlTransaction tx, int phoneVerificationCodesId, string hashCode, int sentCountToday)
        {
            const string sql = """
                UPDATE phone_verification_codes
                SET
                    code_hash = @hashCode,
                    sent_day = CURRENT_DATE,
                    sent_count_today = @sentCountToday,
                    last_sent_at = now(),
                    expires_at = now() + interval '3 minutes'
                WHERE phone_verification_codes_id = @phoneVerificationCodesId
                  AND used = false;
                """;
            
           

            await using var cmd = new NpgsqlCommand( sql, conn, tx);

            cmd.Parameters.Add("@hashCode", NpgsqlDbType.Text).Value = hashCode;
            cmd.Parameters.Add("@sentCountToday", NpgsqlDbType.Integer).Value = sentCountToday;
            cmd.Parameters.Add("@phoneVerificationCodesId", NpgsqlDbType.Bigint).Value = phoneVerificationCodesId; 


            var affected = await cmd.ExecuteNonQueryAsync();

            if (affected != 1)
                throw new BusinessException(
                    "CODE_UPDATE_FAILED",
                    "Verification code update failed"
                );

        }
        public async Task MarkExpiredCodesAsUsed(NpgsqlConnection conn, NpgsqlTransaction tx, string phone)
        {
            const string sql = """
                UPDATE phone_verification_codes
                SET used = true
                WHERE phone_number = @phone
                  AND used = false
                  AND expires_at <= now();
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);
            cmd.Parameters.Add("@phone", NpgsqlDbType.Varchar).Value = phone;

            await cmd.ExecuteNonQueryAsync();
        }

    }
}
