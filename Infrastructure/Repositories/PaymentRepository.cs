using DeliveryAPI.Application.Enums;
using DeliveryAPI.Application.Exeptions;
using DeliveryAPI.Infrastructure.Entity.ReadModel;
using Npgsql;
using NpgsqlTypes;

namespace DeliveryAPI.Infrastructure.Repositories
{
    public class PaymentRepository
    {
        public async Task<int> CreatePayment(
            NpgsqlConnection conn,
            NpgsqlTransaction tx,
            int deliveryId,
            decimal amount)
        {
            const string sql = """
                INSERT INTO payments
                (delivery_id, provider, amount, status_id)
                VALUES
                (@deliveryId, 'liqpay', @amount, @pending)
                RETURNING payment_id
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.Add("@deliveryId", NpgsqlDbType.Integer).Value = deliveryId;
            cmd.Parameters.Add("@amount", NpgsqlDbType.Numeric).Value = amount;
            cmd.Parameters.Add("@pending", NpgsqlDbType.Integer).Value = (int)PaymentStatus.Pending;

            return (int)await cmd.ExecuteScalarAsync();
        }

        public async Task<int> MarkSuccessAndGetDeliveryId(
            NpgsqlConnection conn,
            NpgsqlTransaction tx,
            int paymentId,
            long externalId)
        {
            const string sql = """
                UPDATE payments
                SET status_id = 1,
                    external_payment_id = @externalId,
                    updated_at = NOW()
                WHERE payment_id = @paymentId
                RETURNING delivery_id;
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.Add("@paymentId", NpgsqlDbType.Integer).Value = paymentId;
            cmd.Parameters.Add("@externalId", NpgsqlDbType.Bigint).Value = externalId;

            var result = await cmd.ExecuteScalarAsync();

            if (result == null)
                throw new BusinessException("PAYMENT_NOT_FOUND", "Payment not found");

            return (int)result;
        }

        public async Task<Payment?> GetPendingPayment(
            NpgsqlConnection conn,
            NpgsqlTransaction tx,
            int deliveryId)
        {
            const string sql = """
                SELECT payment_id, amount
                FROM payments
                WHERE delivery_id = @deliveryId
                AND status_id = 0
                LIMIT 1
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.Add("@deliveryId", NpgsqlDbType.Integer).Value = deliveryId;

            await using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return new Payment
            {
                PaymentId = reader.GetInt32(0),
                Amount = reader.GetDecimal(1)
            };
        }

        public async Task<bool> IsAlreadyPaid(
            NpgsqlConnection conn,
            NpgsqlTransaction tx,
            int paymentId)
        {
            const string sql = """
                SELECT 1
                FROM payments
                WHERE payment_id = @paymentId
                AND status_id = 1
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.Add("@paymentId", NpgsqlDbType.Integer).Value = paymentId;

            return await cmd.ExecuteScalarAsync() != null;
        }

        public async Task<Payment> GetById(
            NpgsqlConnection conn,
            NpgsqlTransaction tx,
            int paymentId)
        {
            const string sql = """
                SELECT payment_id, amount, status_id, delivery_id
                FROM payments
                WHERE payment_id = @paymentId
            """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.Add("@paymentId", NpgsqlDbType.Integer).Value = paymentId;

            await using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return new Payment
            {
                PaymentId = reader.GetInt32(0),
                Amount = reader.GetDecimal(1),
                Status = reader.GetInt32(2),
                DeliveryId = reader.GetInt32(3)
            };
        }
    }
}
