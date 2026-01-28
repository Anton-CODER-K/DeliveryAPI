using System.Numerics;
using System.Xml.Linq;
using DeliveryAPI.Api.Middleware;
using DeliveryAPI.Application.Enums.Auth;
using DeliveryAPI.Application.Models.Result;
using DeliveryAPI.Infrastructure.Entity.ReadModel;
using DeliveryAPI.Infrastructure.Entity.Record;
using Microsoft.AspNetCore.Identity;
using Npgsql;
using NpgsqlTypes;

namespace DeliveryAPI.Infrastructure.Repositories
{
    public class AuthRepository
    {
        public async Task<int> UserRegister(NpgsqlConnection conn, NpgsqlTransaction tx, string phone)
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
            ).Value = phone;



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
        public async Task UpdateLoginCode(NpgsqlConnection conn, NpgsqlTransaction tx, int phoneVerificationCodesId, string hashCode)
        {
            const string sql = """
                UPDATE phone_verification_codes
                SET
                    code_hash = @hashCode,
                    sent_day = CURRENT_DATE,
                    sent_count_today = 
                    CASE
                        WHEN sent_day = CURRENT_DATE THEN sent_count_today + 1
                        ELSE 1
                    END,
                    last_sent_at = now(),
                    expires_at = now() + interval '3 minutes'

                WHERE phone_verification_codes_id = @phoneVerificationCodesId
                AND used = false
                AND (
                      sent_day <> CURRENT_DATE
                      OR sent_count_today < 5
                    );
                """;
            
           

            await using var cmd = new NpgsqlCommand( sql, conn, tx);

            cmd.Parameters.Add("@hashCode", NpgsqlDbType.Text).Value = hashCode;
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
        public async Task<ActiveVerificationCodeForVerifyResult?> GetActiveCodeForVerifyByPhone(NpgsqlConnection conn, NpgsqlTransaction tx, string phone)
        {
            const string sql = """
                SELECT
                    phone_verification_codes_id,
                    user_id,
                    code_hash,
                    expires_at,
                    attempts_left
                FROM phone_verification_codes
                WHERE phone_number = @phone
                  AND used = false
                ORDER BY last_sent_at DESC
                LIMIT 1;
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);
             
            cmd.Parameters.Add("@phone", NpgsqlDbType.Varchar).Value = phone;

            await using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new ActiveVerificationCodeForVerifyResult
                {
                    PhoneVerificationCodeId = reader.GetInt32(0),
                    UserId = reader.GetInt32(1),
                    CodeHash = reader.GetString(2),
                    ExpiresAt = reader.GetDateTime(3),
                    AttemptsLeft = reader.GetInt32(4)
                };
            }

            return null;
        }
        public async Task DecrementCodeAttempts(NpgsqlConnection conn, NpgsqlTransaction tx, int phoneVerificationCodeId)
        {
            const string sql = """
                UPDATE phone_verification_codes
                SET attempts_left = attempts_left - 1
                WHERE phone_verification_codes_id = @phoneVerificationCodeId
                  AND used = false;
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);
            cmd.Parameters.Add("@phoneVerificationCodeId", NpgsqlDbType.Integer).Value = phoneVerificationCodeId;

            await cmd.ExecuteNonQueryAsync();
        }
        public async Task MarkCodeAsUsed(NpgsqlConnection conn, NpgsqlTransaction tx, int phoneVerificationCodeId)
        {
            const string sql = """
                UPDATE phone_verification_codes
                SET used = true
                WHERE phone_verification_codes_id = @phoneVerificationCodeId
                  AND used = false;
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.Add("@phoneVerificationCodeId", NpgsqlDbType.Integer).Value = phoneVerificationCodeId;

            await cmd.ExecuteNonQueryAsync();
        }
        public async Task<string> GetRolesUserPhone(NpgsqlConnection conn, NpgsqlTransaction tx, int userId)
        {
            const string sql = """
                SELECT r.name
                FROM users u
                JOIN roles r ON r.role_id = u.role_id
                WHERE u.user_id = @userId;
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.Add("@userId", NpgsqlDbType.Integer).Value = userId;

            await using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return reader.GetString(0);
            }

            throw new BusinessException("INSERT_ERROR", "Internal server error ");
        }
        public async Task CreateVerificationSession(NpgsqlConnection conn, NpgsqlTransaction tx, int userId, string tokenHash, string purpose)
        {
            const string sql = """
                Insert Into verification_sessions (user_id, token_hash, purpose)
                Values (@userId, @tokenHash, @purpose)
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.Add("@userId", NpgsqlDbType.Bigint).Value = userId;
            cmd.Parameters.Add("@tokenHash", NpgsqlDbType.Text).Value = tokenHash;
            cmd.Parameters.Add("@purpose", NpgsqlDbType.Varchar).Value = purpose;

            await cmd.ExecuteNonQueryAsync();
        }
    }
}
