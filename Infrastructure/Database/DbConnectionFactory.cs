using Npgsql;

namespace DeliveryAPI.Infrastructure.Database
{
    public interface IDbConnectionFactory
    {
        NpgsqlConnection Create();
    }

    public class DbConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public DbConnectionFactory(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("Default")
                ?? throw new ArgumentNullException("Connection string not found");
        }

        public NpgsqlConnection Create()
        {
            return new NpgsqlConnection(_connectionString);
        }
    }

}
