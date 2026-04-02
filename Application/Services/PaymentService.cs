using DeliveryAPI.Api.Contracts.Request;
using System.Transactions;
using DeliveryAPI.Api.Contracts.Response;
using DeliveryAPI.Infrastructure.Database;
using DeliveryAPI.Infrastructure.Repositories;
using DeliveryAPI.Application.Exeptions;
using DeliveryAPI.Application.Enums;
using Npgsql;

namespace DeliveryAPI.Application.Services
{
    public class PaymentService
    {
        private readonly PaymentRepository _paymentRepo;
        private readonly LiqPayService _liqPay;
        private readonly TransactionExecutor _tx;
        private readonly DeliveryRepository _deliveryRepo;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(
            PaymentRepository paymentRepo,
            LiqPayService liqPay,
            TransactionExecutor tx,
            DeliveryRepository deliveryRepo,
            ILogger<PaymentService> logger)
        {
            _paymentRepo = paymentRepo;
            _liqPay = liqPay;
            _tx = tx;
            _deliveryRepo = deliveryRepo;
            _logger = logger;
        }

        public async Task<LiqPayCheckoutResponse> CreatePayment(int deliveryId, int userId)
        {
            return await _tx.ExecuteAsync(async (conn, tx) =>
            {
                var delivery = await _deliveryRepo.GetDeliveryPayment(conn, tx, deliveryId);

                if (delivery == null || delivery.UserId != userId)
                    throw new BusinessException("DELIVERY_NOT_FOUND", "Delivery not found");

                if (delivery.PaymentMethod == (int)PaymentsMethod.Cash)
                    throw new BusinessException("CASH_PAYMENT", "Delivery is marked for cash payment");

                var payment = await _paymentRepo.GetByDeliveryId(conn, tx, deliveryId);

                if (payment != null)
                {
                    if (payment.Status == (int)PaymentStatus.Success)
                        throw new BusinessException("ALREADY_PAID", "Delivery is already paid");

                    return _liqPay.CreateCheckout(payment.PaymentId, payment.Amount);
                }

                var paymentId = await _paymentRepo.CreatePayment(
                    conn,
                    tx,
                    deliveryId,
                    delivery.TotalPrice,
                    "liqpay");

                return _liqPay.CreateCheckout(paymentId, delivery.TotalPrice);
            });
        }

        public async Task CreateCashPayment(NpgsqlConnection conn, NpgsqlTransaction tx, int deliveryId, decimal totalAmount)
        {
            await _paymentRepo.CreatePayment(conn, tx, deliveryId, totalAmount, "cash");
        }

        public async Task HandleWebhook(LiqPayWebhookRequest request)
        {
            if (!_liqPay.ValidateSignature(request.Data, request.Signature))
                throw new BusinessException("INVALID_SIGNATURE", "Invalid signature");

            var webhook = _liqPay.ParseWebhook(request.Data);

            if (webhook.status != "success" && webhook.status != "sandbox")
                return;

            int paymentId = int.Parse(webhook.order_id);

            await _tx.ExecuteAsync(async (conn, tx) =>
            {
                var payment = await _paymentRepo.GetById(conn, tx, paymentId);

                if (payment == null)
                    throw new BusinessException("PAYMENT_NOT_FOUND", "Payment not found");


                if (Math.Abs(payment.Amount - webhook.amount) > 0.01m)
                    throw new BusinessException("INVALID_AMOUNT", "Invalid amount");

                if (payment.Status != (int)PaymentStatus.Pending) 
                    return;

                var deliveryId = await _paymentRepo.MarkSuccessAndGetDeliveryId(
                    conn,
                    tx,
                    paymentId,
                    webhook.payment_id);

                //await _deliveryRepo.UpdateStatus(
                //    conn,
                //    tx,
                //    deliveryId,
                //    DeliveryStatus.Paid);
            });
        }
    }
}

