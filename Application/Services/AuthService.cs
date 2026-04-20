using DeliveryAPI.Api.Contracts.Request;
using DeliveryAPI.Api.Middleware;
using DeliveryAPI.Application.Exeptions;
using DeliveryAPI.Application.FakeSmsSender;
using DeliveryAPI.Application.Interfaces;
using DeliveryAPI.Application.Models.Input;
using DeliveryAPI.Application.Models.Result;
using DeliveryAPI.Application.Verification;
using DeliveryAPI.Infrastructure.Database;
using DeliveryAPI.Infrastructure.Entity.ReadModel;
using DeliveryAPI.Infrastructure.Entity.Record;
using DeliveryAPI.Infrastructure.Repositories;
using DeliveryAPI.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Npgsql;
using SixLabors.ImageSharp;
using System.Data;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace DeliveryAPI.Application.Services
{
    public class AuthService
    {
        private readonly JwtService _jwtService;
        private readonly AuthRepository _authRepo;
        private readonly TransactionExecutor _tx;
        private readonly IVerificationCodeGenerator _codeGenerator;
        private readonly IVerificationMessageBuilder _messageBuilder;
        private readonly INotificationSender _notificationSender;
        private readonly IImageStorage _imageStorage;
        private readonly ILogger<DeliveryService> _logger;

        public AuthService(JwtService jwtService, AuthRepository AuthRepo, TransactionExecutor transactionExecutor, IVerificationCodeGenerator codeGenerator, IVerificationMessageBuilder messageBuilder, INotificationSender notificationSender, ILogger<DeliveryService> logger, IImageStorage imageStorage)
        {
            _jwtService = jwtService;
            _authRepo = AuthRepo;
            _tx = transactionExecutor;
            _codeGenerator = codeGenerator;
            _messageBuilder = messageBuilder;
            _notificationSender = notificationSender;
            _imageStorage = imageStorage;
            _logger = logger;
        }

        public async Task StartAsync(string rawPhone)
        {
            var phone = NormalizeToE164(rawPhone);

            if (!IsValidE164(phone))
                throw new BusinessException("INVALID_PHONE", "Invalid phone number");

            var code = _codeGenerator.Generate();
            var message = _messageBuilder.Build(code);
            var hashCode = HashVerificationCode(code);
            var now = DateTime.UtcNow;

            bool shouldSendSms = false;

            await _tx.ExecuteAsync(async (conn, tx) =>
            {
                var userId =
                    await _authRepo.GetUserIdByPhone(conn, tx, phone)
                    ?? await _authRepo.UserRegister(conn, tx, phone);

                // If code is received but not used, He its update
                await _authRepo.MarkExpiredCodesAsUsed(conn, tx, phone);

                var activeCode = await _authRepo.GetActiveCodeByPhone(conn, tx, phone);

                if (activeCode == null)
                {
                    await _authRepo.InsertLoginCode(conn, tx, phone, hashCode, userId);
                    shouldSendSms = true;
                    return;
                }

                
                if (now < activeCode.LastSentAt.AddSeconds(60))
                    throw new BusinessException(
                        "CODE_ALREADY_SENT",
                        "Please wait before requesting another code"
                    );

                
                var today = DateOnly.FromDateTime(now);

                if (activeCode.SentDay == today)
                {
                    if (activeCode.SentCountToday >= 5)
                        throw new BusinessException(
                            "SMS_LIMIT_REACHED",
                            "Try again tomorrow"
                        );
                }
                else
                {
                    activeCode.SentDay = today;
                    
                }
                await _authRepo.UpdateLoginCode(conn, tx, activeCode.phoneVerificationCodesId, hashCode);
                shouldSendSms = true;
            });

            if (shouldSendSms)
                await _notificationSender.SendAsync(phone, message);

            _logger.LogInformation("Number {NumberPhone} Created", phone);

        }

        public async Task<string> VerifyAsync(string rawPhone, string code)
        {
            var phone = NormalizeToE164(rawPhone);
            var now = DateTime.UtcNow;
            var inputHash = HashVerificationCode(code);
            string token = string.Empty;

            await _tx.ExecuteAsync(async (conn, tx) =>
            {
                var activeCode =
                    await _authRepo.GetActiveCodeForVerifyByPhone(conn, tx, phone);

                if (activeCode == null)
                    throw new BusinessException("INVALID_CODE", "Invalid code");

                if (activeCode.ExpiresAt <= now)
                    throw new BusinessException("INVALID_CODE", "Invalid code");

                if (activeCode.CodeHash != inputHash)
                {
                    if (activeCode.AttemptsLeft <= 0)
                        throw new BusinessException("INVALID_CODE", "Invalid code");

                    await _authRepo.DecrementCodeAttempts( conn, tx, activeCode.PhoneVerificationCodeId);

                    throw new BusinessException("INVALID_CODE", "Invalid code");
                }

                token = GenerateToken();
                var tokenHash = HashToken(token);

                await _authRepo.MarkCodeAsUsed(conn, tx, activeCode.PhoneVerificationCodeId);

                var purpose = Enums.Auth.VerificationSessionsPurpose.Purpose.register;
                string purposeString = purpose.ToString();

                await _authRepo.CreateVerificationSession(conn, tx, activeCode.UserId, tokenHash, purposeString);         
            });

            _logger.LogInformation("Number {NumberPhone} is verifed code", phone);


            return token;
            
        }

        public async Task<TokensResult> SetPasswordByTokenAsync(string token, string password, string name, DateOnly birthday, string? ip, string? userAgent)
        {
            string accessToken = String.Empty;
            string refreshToken = String.Empty;

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            if (birthday > today.AddYears(-14))
                throw new BusinessException("UNDERAGE_USER", "User must be at least 14 years old");

            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
                throw new BusinessException("WEAK_PASSWORD", "Password must be at least 8 characters long");

            if (string.IsNullOrWhiteSpace(name))
                throw new BusinessException("INVALID_NAME", "Name cannot be empty");

            if (name.Length < 3)
                throw new BusinessException("INVALID_NAME", "Name must be at least 3 characters long");

            var tokenHash = HashToken(token);
            int userId = 0;

            await _tx.ExecuteAsync(async (conn, tx) =>
            {
                var purpose = Enums.Auth.VerificationSessionsPurpose.Purpose.register;
                string purposeString = purpose.ToString();

                var session = await _authRepo.GetActiveVerificationSessionByTokenHash(conn, tx, tokenHash, purposeString);

                if (session == null)
                    throw new UnauthorizedException("Invalid refresh token");

                userId = session.UserId;

                await _authRepo.SetUserPassword(conn, tx, session.UserId, HashPassword(password), name, birthday);

                await _authRepo.MarkVerificationSessionUsed(conn, tx, session.VerificationSessionId);

                var roles = await _authRepo.GetRolesUserByUserId(conn, tx, session.UserId);
                accessToken = _jwtService.GenerateAccessToken(session.UserId, roles);

                refreshToken = GenerateToken();
                string refreshTokenHash = HashToken(refreshToken);

                var sessionId = await _authRepo.InsertSession(conn, tx, session.UserId, ip, userAgent);
                await _authRepo.InsertRefreshToken(conn, tx, sessionId, refreshTokenHash);
                
            });

            _logger.LogInformation("User {UserId} set password", userId);


            return new TokensResult
            {
                accessToken = accessToken,
                refreshToken = refreshToken
            };
        }

        public async Task<TokensResult> RefreshAsync(string refreshToken, string? ip, string? userAgent)
        {
            var hash = HashToken(refreshToken);
            string access = String.Empty;
            string newRefresh = String.Empty;
            int userId = 0;

            await _tx.ExecuteAsync(async (conn, tx) =>
            {
                var token = await _authRepo.GetActiveRefreshToken(conn, tx, hash);

                if (token == null)
                    throw new UnauthorizedException();

                userId = token.UserId;

                var affectedRevokeToken = await _authRepo.RevokeRefreshToken(conn, tx, token.Id);

                if (affectedRevokeToken == 0)
                    throw new UnauthorizedException("Refresh token already used or invalid");

                var roles = await _authRepo.GetRolesUserByUserId(conn, tx, token.UserId);

                access = _jwtService.GenerateAccessToken(token.UserId, roles);
                newRefresh = GenerateToken();
                var newHash = HashToken(newRefresh);

                

                await _authRepo.InsertRefreshToken(conn, tx, token.SessionId, newHash);
                
            });

            _logger.LogInformation("User {UserId} refresh token", userId);

            return new TokensResult
            {
                accessToken = access,
                refreshToken = newRefresh
            };
        }

        public async Task<TokensResult> LoginAsync(string phoneNumber, string password, string? ip, string? userAgent)
        {
            UserByLoginResult? user = null;

            var phone = NormalizeToE164(phoneNumber);

            if (!IsValidE164(phone))
                throw new BusinessException("INVALID_PHONE", "Invalid phone number");

            await _tx.ExecuteAsync(async (conn, tx) =>
            {
                user = await _authRepo.GetUserByPhone(conn, tx, phone);
            });

            if (user == null)
                throw new UnauthorizedException("Invalid phone number or password");

            if (string.IsNullOrEmpty(user.HashPassword))
                throw new UnauthorizedException("Invalid phone number or password");

            if (!BCrypt.Net.BCrypt.Verify(password, user.HashPassword))
                throw new UnauthorizedException("Invalid phone number or password");

            var accessToken = _jwtService.GenerateAccessToken(user.UserId, user.Roles);
            var refreshToken = GenerateToken();
            var refreshTokenHash = HashToken(refreshToken);
            await _tx.ExecuteAsync(async (conn, tx) =>
            {
                var sessionId = await _authRepo.InsertSession(conn, tx, user.UserId, ip, userAgent);
                await _authRepo.InsertRefreshToken(conn, tx, sessionId ,refreshTokenHash);
            });

            _logger.LogInformation("User {UserId} is login", user.UserId);

            return new TokensResult
            {
                accessToken = accessToken,
                refreshToken = refreshToken
            };
            
        }

        public async Task<MeResult?> GetMeAsync(int userId, string role)
        {

            var userResult = null as UserByMeResult;
            await _tx.ExecuteAsync(async (conn, tx) =>
            {
                userResult = await _authRepo.GetUserById(conn, tx, userId);
            });

            if (userResult == null)
                throw new UnauthorizedException("User not found");

            return new MeResult
            {
                UserId = userResult.UserId,
                Phone = userResult.Phone,
                Name = userResult.Name,
                AvatarUrl = "http://37.27.220.44/images/" + userResult.AvatarUrl + "/thumb.jpg",
                Role = role
            };
        }

        public async Task UploadPhotoAsync(int userId, IFormFile image)
        {
            LocalImageStorage.IsImage(image);

            string? newImageFolder = null;
            int result = 0;

            await _tx.ExecuteAsync(async (conn, tx) =>
            {
                try
                {
                    var oldImageFolder = await _authRepo.GetUserById(conn, tx, userId);

                    await using var stream = image.OpenReadStream();

                    var imageResult = await _imageStorage.SaveImageAsync(stream, "users");

                    newImageFolder = imageResult.Folder;

                    await _authRepo.UpdateUserPathFolder(conn, tx, userId, newImageFolder);

                    if (!string.IsNullOrEmpty(oldImageFolder.AvatarUrl))
                    {
                        await _imageStorage.DeleteImageAsync(oldImageFolder.AvatarUrl);
                    }
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

        }





        //public async Task<List<GetSessionsResult>> GetSessionsUserIdAsync(int userId)
        //{

        //}

        //public async Task<AuthTokensResponse> CompleteRegistrationAsync(string verificationToken, AuthSetPasswordRequest request, string ip, string userAgent)
        //{
        //    // 1. verify verification token
        //    var userId = await _verificationService.VerifyAsync(verificationToken);

        //    // 2. set password + profile
        //    await _userService.SetPasswordAndProfile(userId, request);

        //    // 3. get roles
        //    var roles = await _authRepo.GetRolesUserPhone(...);

        //    // 4. generate tokens
        //    return await _tokenService.IssueTokensAsync(
        //        userId, roles, ip, userAgent
        //    );
        //}




        private static string NormalizeToE164(string input)
        {
            var phone = Regex.Replace(input, @"[\s\-\(\)]", "");

            if (phone.StartsWith("0") && phone.Length == 10)
                return "+380" + phone[1..];

            if (phone.StartsWith("380") && phone.Length == 12)
                return "+" + phone;

            if (phone.StartsWith("+380") && phone.Length == 13)
                return phone;

            throw new BusinessException("INVALID_PHONE", "Unsupported phone format");
        }

        private static bool IsValidE164(string phone)
        {
            return Regex.IsMatch(phone, @"^\+380\d{9}$");
        }

        private static string HashPassword(string password)
        {
            string hashPassword = BCrypt.Net.BCrypt.HashPassword(password);
            return hashPassword;

        }

        private static string HashVerificationCode(string code)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(code);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToHexString(hash); 
        }
        
        private static string GenerateToken(int bytes = 32)
        {
            var buffer = new byte[bytes];
            RandomNumberGenerator.Fill(buffer);
            return Convert.ToBase64String(buffer)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }

        private static string HashToken(string token)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(token);
            return Convert.ToHexString(sha.ComputeHash(bytes));
        }
    }

    

}
