using System.Security.Claims;
using DeliveryAPI.Api.Contracts.Response;
using DeliveryAPI.Api.Middleware;
using DeliveryAPI.Application.Exeptions;
using DeliveryAPI.Application.Models.Input;
using DeliveryAPI.Infrastructure.Database;
using DeliveryAPI.Infrastructure.Entity.Record;
using DeliveryAPI.Infrastructure.Repositories;
using Npgsql;

namespace DeliveryAPI.Application.Services
{
    public class AddressService
    {
        private readonly TransactionExecutor _tx;
        private readonly AddressRepository _addressRepository;

        public AddressService(TransactionExecutor tx, AddressRepository addressRepository)
        {
            _tx = tx;
            _addressRepository = addressRepository;
        }


        public async Task<int> CreateAddressAsync(AddressCreateServiceInput Input)
        {
            int addressId = 0;

            await _tx.ExecuteAsync(async (conn, tx) =>
            {
                if (await _addressRepository.CheckLimitAddressByUserId(conn, tx, Input.userId) >= 10)
                {
                    throw new BusinessException("ADDRESS LIMIT", "Address limit reached");
                }

                if (Input.is_default)
                {
                    await _addressRepository.MarkAllAddressByUserNotDefault(conn, tx, Input.userId);
                }

                addressId = await _addressRepository.InsertAddress(conn, tx, new AddressCreateRepo
                {
                    userId = Input.userId,
                    title = Input.title,
                    latitude = Input.latitude,
                    longitude = Input.longitude,
                    house = Input.house,
                    apartment = Input.apartment,
                    entrance = Input.entrance,
                    floor = Input.floor,
                    comment = Input.comment,
                    is_default = Input.is_default,
                });
            });

            return addressId;
        }

        public async Task<List<AddressUserIdResponse>> GetAddressByUserIdAsync(int userId)
        {
            List<AddressUserIdResponse> addressUserIdResponses = new List<AddressUserIdResponse>();
            await _tx.ExecuteAsync(async (conn, tx) =>
            {
                addressUserIdResponses = await _addressRepository.GetAddressByUserId(conn, tx, userId);
            });

            return addressUserIdResponses;
        }

        public async Task SetDefaultAddressAsync(int userId, int id)
        {
            await _tx.ExecuteAsync(async (conn, tx) =>
            {
                await _addressRepository.MarkAllAddressByUserNotDefault(conn, tx, userId);
                var affected = await _addressRepository.SetDefaultAddress(conn, tx, userId, id);

                if (affected == 0)
                    throw new BusinessException("ADDRESS_NOT_FOUND", "Address not found");
            });
        }

        public async Task DeleteAddressAsync(int userId, int addressId)
        {
            await _tx.ExecuteAsync(async (conn, tx) =>
            {
                var affected = await _addressRepository.DeleteAddress(conn, tx, userId, addressId);
                if (affected == 0)
                    throw new BusinessException("ADDRESS_NOT_FOUND", "Address not found");
            });
        }
    }
}
