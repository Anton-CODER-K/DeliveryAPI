using DeliveryAPI.Application.Models.Input;
using DeliveryAPI.Infrastructure.Database;
using DeliveryAPI.Infrastructure.Repositories;
using DeliveryAPI.Infrastructure.Entity.Record;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using DeliveryAPI.Api.Middleware;
using Npgsql;
using DeliveryAPI.Application.FakeSmsSender;
using DeliveryAPI.Application.Verification;
using System.Text;
using System.Security.Cryptography;

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

        public AuthService(JwtService jwtService, AuthRepository AuthRepo, TransactionExecutor transactionExecutor, IVerificationCodeGenerator codeGenerator, IVerificationMessageBuilder messageBuilder, INotificationSender notificationSender)
        {
            _jwtService = jwtService;
            _authRepo = AuthRepo;
            _tx = transactionExecutor;
            _codeGenerator = codeGenerator;
            _messageBuilder = messageBuilder;
            _notificationSender = notificationSender;
        }

        //public async Task UserRegister(UserRegisterInput user)
        //{
        //    if (!IsValidE164(user.Number_Phone))
        //        throw new BusinessException("INVALID_PHONE", "Incorect input number phone");



        //    await _tx.ExecuteAsync(async (conn, tx) =>
        //    {
        //        try
        //        {
        //            await _authRepo.UserRegister(conn, tx, new UserRegisterRecord
        //            {
        //                Number_Phone = user.Number_Phone,
        //                Name = user.Name,
        //                Password_Hash = HashPassword(user.Password),
        //                Birthday = user.Birthday,
        //            });
        //        }
        //        catch (PostgresException ex)
        //        when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        //        {
        //            throw new BusinessException("PHONE_ALREADY_EXISTS", "Phone alredy exists");
        //        }

        //    });
        //}

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

                    activeCode.SentCountToday++;
                }
                else
                {
                    activeCode.SentDay = today;
                    activeCode.SentCountToday = 1;
                }

                await _authRepo.UpdateLoginCode(
                    conn,
                    tx,
                    activeCode.phoneVerificationCodesId,
                    hashCode,
                    activeCode.SentCountToday
                );

                shouldSendSms = true;
            });

            if (shouldSendSms)
                await _notificationSender.SendAsync(phone, message);
        }

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
    }

    

}
