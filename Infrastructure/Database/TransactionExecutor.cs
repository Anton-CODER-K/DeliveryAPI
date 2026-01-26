using Npgsql;

namespace DeliveryAPI.Infrastructure.Database
{
    public class TransactionExecutor
    {
        private readonly IDbConnectionFactory _factory;

        public TransactionExecutor(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task ExecuteAsync(
            Func<NpgsqlConnection, NpgsqlTransaction, Task> action)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync();

            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                await action(conn, tx);
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

       
        public async Task<T> ExecuteAsync<T>(
            Func<NpgsqlConnection, Task<T>> action)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync();
            return await action(conn);
        }
    }
}
