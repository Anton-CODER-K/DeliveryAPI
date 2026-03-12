using DeliveryAPI.Api.Contracts.Response;
using DeliveryAPI.Application.Models.Result;
using DeliveryAPI.Infrastructure.Database;
using DeliveryAPI.Infrastructure.Repositories;

namespace DeliveryAPI.Application.Services
{
    public class UserService
    {
        private readonly TransactionExecutor _tx;
        private readonly UserRepository _userRepo;


        public UserService (TransactionExecutor tx, UserRepository userRepo)
        {
            _tx = tx;
            _userRepo = userRepo;
        }

        public async Task<List<Users>> GetUsers(int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 50) pageSize = 50;

            int offset = (page - 1) * pageSize;

            List<Users> users = new List<Users>();

            await _tx.ExecuteAsync(async (conn, tx) =>
            {
                users = await _userRepo.GetUsers(conn, tx, offset, pageSize);
            });

            return users;
        }


    }
}
