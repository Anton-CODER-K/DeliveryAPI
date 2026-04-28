using System.Dynamic;
using System.Numerics;
using System.Xml.Linq;
using DeliveryAPI.Api.Middleware;
using DeliveryAPI.Application.Enums.Auth;
using DeliveryAPI.Application.Exeptions;
using DeliveryAPI.Application.Models.Result;
using DeliveryAPI.Infrastructure.Entity.ReadModel;
using DeliveryAPI.Infrastructure.Entity.Record;
using Microsoft.AspNetCore.Identity;
using Npgsql;
using NpgsqlTypes;
using static DeliveryAPI.Application.Enums.Auth.VerificationSessionsPurpose;

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

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

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
        public async Task<UserByLoginResult> GetUserByPhone(NpgsqlConnection conn, NpgsqlTransaction tx, string phoneNumber)
        {
            const string sql = """
                SELECT u.user_id, r.name, u.password_hash
                FROM users u
                Join roles r on r.role_id = u.role_id
                WHERE phone_number = @phone and is_active = true
                Limit 1;
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);
            cmd.Parameters.Add("@phone", NpgsqlDbType.Varchar).Value = phoneNumber;

            await using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new UserByLoginResult
                {
                    UserId = reader.GetInt32(0),
                    Roles = reader.GetString(1),
                    HashPassword = reader.IsDBNull(2) ? null : reader.GetString(2)
                };
            }

            return null;

        }
        public async Task InsertLoginCode(NpgsqlConnection conn, NpgsqlTransaction tx, string phone, string hashCode, int userId)
        {
            const string sql = """
                INSERT INTO phone_verification_codes
                    (user_id, phone_number, code_hash, expires_at, attempts_left, last_sent_at)
                VALUES
                    (@userId, @phone, @codeHash, now() + interval '3 minutes', 3, now());
                
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

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



            await using var cmd = new NpgsqlCommand(sql, conn, tx);

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
        public async Task<string> GetRolesUserByUserId(NpgsqlConnection conn, NpgsqlTransaction tx, int userId)
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

            return null;
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
        public async Task<GetActiveVerificationSessionByTokenHashReadModel> GetActiveVerificationSessionByTokenHash(NpgsqlConnection conn, NpgsqlTransaction tx, string token, string purpose)
        {
            const string sql = """
                SELECT
                    verification_sessions_id,
                    user_id
                FROM verification_sessions
                WHERE token_hash = @hash
                  AND used = false
                  AND expires_at > now()
                  AND purpose = @purpose
                Limit 1;
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.Add("@hash", NpgsqlDbType.Text).Value = token;
            cmd.Parameters.Add("@purpose", NpgsqlDbType.Varchar).Value = purpose;

            await using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new GetActiveVerificationSessionByTokenHashReadModel
                {
                    VerificationSessionId = reader.GetInt32(0),
                    UserId = reader.GetInt32(1),
                };
            }

            return null;
        }
        public async Task SetUserPassword(NpgsqlConnection conn, NpgsqlTransaction tx, int userId, string passwordHash, string name, DateOnly birthday)
        {
            const string sql = """
                UPDATE users
                SET password_hash = @passwordHash,
                    is_phone_verified = true,
                    name = @name,
                    birth_date = @birthday
                WHERE user_id = @userId
                    and password_hash is null
                    and is_phone_verified = false;
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.Add("@passwordHash", NpgsqlDbType.Text).Value = passwordHash;
            cmd.Parameters.Add("@userId", NpgsqlDbType.Integer).Value = userId;
            cmd.Parameters.Add("@name", NpgsqlDbType.Varchar).Value = name;
            cmd.Parameters.Add("@birthday", NpgsqlDbType.Date).Value = birthday;

            var affected = await cmd.ExecuteNonQueryAsync();

            if (affected != 1)
            {
                throw new BusinessException(
                    "PASSWORD_ALREADY_SET",
                    "Password already set or phone already verified"
                );
            }

        }
        public async Task MarkVerificationSessionUsed(NpgsqlConnection conn, NpgsqlTransaction tx, int verificationSessionId)
        {
            const string sql = """
                UPDATE verification_sessions
                SET used = true
                WHERE verification_sessions_id = @verificationSessionId
                  AND used = false;
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);
            cmd.Parameters.Add("@verificationSessionId", NpgsqlDbType.Integer).Value = verificationSessionId;
            var affected = await cmd.ExecuteNonQueryAsync();

            if (affected != 1)
            {
                throw new BusinessException(
                    "SESSION_ALREADY_USED",
                    "Verification session already used or invalid"
                );
            }

        }
        public async Task InsertRefreshToken(NpgsqlConnection conn, NpgsqlTransaction tx, int SessionId, string refreshTokenHash)
        {
            const string sql = """
                Insert Into refresh_tokens
                    (session_id, token_hash, expires_at, revoked_at, created_at)
                Values
                    (@SessionId, @tokenHash, now() + interval '14 days', null, now());
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);
            cmd.Parameters.Add("@SessionId", NpgsqlDbType.Integer).Value = SessionId;
            cmd.Parameters.Add("@tokenHash", NpgsqlDbType.Text).Value = refreshTokenHash;
           

            await cmd.ExecuteNonQueryAsync();
        }
        public async Task<int> InsertSession(NpgsqlConnection conn, NpgsqlTransaction tx, int userId, string? ip, string? userAgent)
        {
            const string sql = """
                Insert Into sessions
                    (user_id, ip_address, user_agent)
                Values
                    (@userId, @ip, @userAgent)
                returning session_id;
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);
            cmd.Parameters.Add("@userId", NpgsqlDbType.Integer).Value = userId;
            cmd.Parameters.Add("@ip", NpgsqlDbType.Varchar).Value = (object?)ip ?? DBNull.Value;
            cmd.Parameters.Add("@userAgent", NpgsqlDbType.Varchar).Value = (object?)userAgent ?? DBNull.Value;

            var result = await cmd.ExecuteScalarAsync();
            if (result == null)
                throw new BusinessException("SESSION_CREATE_FAILED", "Failed to create session");

            return (int)result;


        }
        //internal async Task<int> GetActiveSessionByTokenHash(NpgsqlConnection conn, NpgsqlTransaction tx, int SessionId)
        //{
        //    const string sql = """
        //        Select session_id
        //        From sessions
        //        Where 
        //        """;
        //}
        public async Task<ActiveRefreshTokenResult?> GetActiveRefreshToken(NpgsqlConnection conn, NpgsqlTransaction tx, string hash)
        {
            const string sql = """
                SELECT s.user_id, rt.refresh_token_id, rt.session_id
                FROM refresh_tokens rt
                JOIN sessions s on s.session_id = rt.session_id 
                WHERE rt.token_hash = @hash
                  AND rt.revoked_at IS NULL
                  AND rt.expires_at > now()
                LIMIT 1;
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);
            cmd.Parameters.Add("@hash", NpgsqlDbType.Text).Value = hash;

            await using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new ActiveRefreshTokenResult
                {
                    UserId = reader.GetInt32(0),
                    Id = reader.GetInt32(1),
                    SessionId = reader.GetInt32(2)
                };
                
            }

            return null;
        }
        public async Task<int> RevokeRefreshToken(NpgsqlConnection conn, NpgsqlTransaction tx, int id)
        {
            const string sql = """
                UPDATE refresh_tokens
                SET revoked_at = now()
                WHERE refresh_token_id = @id
                  AND revoked_at IS NULL;
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);
            cmd.Parameters.Add("@id", NpgsqlDbType.Integer).Value = id;
            
            return await cmd.ExecuteNonQueryAsync();

        }
        public async Task<UserByMeResult?> GetUserById(NpgsqlConnection conn, NpgsqlTransaction tx, int userId)
        {
            const string sql = """
                Select 
                    u.user_id,
                    u.phone_number,
                    u.name,
                    i.folder as avatar_url
                From users u
                Left Join images i on i.image_id = u.image_id
                Where u.user_id = @userId
                    and u.is_active = true
                    and u.is_phone_verified = true
                Limit 1;
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);
            cmd.Parameters.Add("@userId", NpgsqlDbType.Integer).Value = userId;

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new UserByMeResult
                {
                    UserId = reader.GetInt32(0),
                    Phone = reader.GetString(1),
                    Name = reader.GetString(2),
                    AvatarUrl = reader.IsDBNull(3) ? null : reader.GetString(3)
                };
            }

            return null;
        }

        public async Task<int> UpdateUserPathFolder(NpgsqlConnection conn, NpgsqlTransaction tx, int userId, string folder)
        {
            const string sql = """
                WITH new_image AS (
                    INSERT INTO images (folder)
                    SELECT @folder
                    FROM users u
                    WHERE u.user_id = @userId
                      AND u.image_id IS NULL
                    RETURNING image_id
                ),
                updated_user AS (
                    UPDATE users u
                    SET image_id = COALESCE(
                        (SELECT image_id FROM new_image),
                        u.image_id
                    )
                    WHERE u.user_id = @userId
                    RETURNING u.image_id
                )
                UPDATE images i
                SET folder = @folder
                WHERE i.image_id = (SELECT image_id FROM updated_user);
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);
            cmd.Parameters.Add("@userId", NpgsqlDbType.Integer).Value = userId;
            cmd.Parameters.Add("@folder", NpgsqlDbType.Varchar).Value = folder;

            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteUserById(NpgsqlConnection conn, NpgsqlTransaction tx, int userId)
        {
            const string sql = """
                Update users
                Set is_active = false,
                    deleted_at = now(),
                    phone_number = null,
                    name = 'Deleted User'
                    password_hash = null,
                    is_phone_verified = false
                Where user_id = @userId
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);
            cmd.Parameters.Add("@userId", NpgsqlDbType.Integer).Value = userId;

            await cmd.ExecuteNonQueryAsync();
        }
    }
}
