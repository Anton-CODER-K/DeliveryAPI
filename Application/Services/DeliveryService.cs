
using DeliveryAPI.Api.Contracts.Response;
using DeliveryAPI.Application.Enums;
using DeliveryAPI.Application.Exeptions;
using DeliveryAPI.Application.Models.Input;
using DeliveryAPI.Application.Models.Result;
using DeliveryAPI.Infrastructure.Database;
using DeliveryAPI.Infrastructure.Repositories;

namespace DeliveryAPI.Application.Services
{
    public class DeliveryService
    {
        private readonly TransactionExecutor _tx;
        private readonly DeliveryRepository _deliveryRepo;
        private readonly AddressRepository _addressRepo;
        private readonly ProductRepository _productRepo;
        

        public DeliveryService(TransactionExecutor tx, DeliveryRepository deliveryRepository, AddressRepository addressRepository, ProductRepository productRepo)
        {
            _tx = tx;
            _deliveryRepo = deliveryRepository;
            _addressRepo = addressRepository;
            _productRepo = productRepo;
        }



        public async Task<int> CreateAsync(DeliveryCreateInput input)
        {
            int result = 0;

            if (input.PaymentMethodId < 1 && input.PaymentMethodId > 2)
                throw new BusinessException("INVALID_PAYMENT_METHOD", "Invalid payment method");

            await _tx.ExecuteAsync(async (conn, tx) =>
            {
                // 1. Перевірити адресу
                var address = await _addressRepo.GetUserAddress(conn, tx, input.AddressId, input.UserId);
                if (address == null)
                    throw new BusinessException("ADDRESS_NOT_FOUND", "Address not found");

                // 2. Отримати продукти
                var products = await _productRepo.GetProductsByIds(conn, tx, input.Products);

                if (products.Count == 0)
                    throw new BusinessException("NO_PRODUCTS", "No valid products");

                // 3. Перевірити що всі з одного ресторану
                int restaurantId = products.First().RestaurantId;

                if (products.Any(p => p.RestaurantId != restaurantId))
                    throw new BusinessException("ONE_RESTAURANT_ONLY", "Products must belong to one restaurant");

                // 4. Розрахунки
                decimal subtotal = 0;
                int totalWeight = 0;

                foreach (var p in products)
                {
                    subtotal += p.Price * p.Quantity;
                    totalWeight += p.WeightGrams * p.Quantity;
                }

                if (totalWeight > 10000)
                    throw new BusinessException("TOO_HEAVY", "Maximum weight is 10kg");

                decimal deliveryFee = CalculateDeliveryFee(totalWeight);

                var restaurant = await _deliveryRepo.GetRestaurant(conn, tx, restaurantId);

                if (restaurant == null)
                    throw new Exception();

                decimal commissionPercent = restaurant.CommissionPercent;
                decimal commissionAmount = subtotal * commissionPercent;

                decimal totalAmount = subtotal + deliveryFee;

                // 5. Створити delivery
                int deliveryId = await _deliveryRepo.InsertDelivery(
                    conn, tx,
                    input.UserId,
                    restaurantId,
                    subtotal,
                    deliveryFee,
                    commissionPercent,
                    commissionAmount,
                    totalAmount,
                    totalWeight,
                    input.PaymentMethodId
                );

                // 6. Створити delivery_items snapshot
                foreach (var p in products)
                {
                    decimal totalLine = p.Price * p.Quantity;

                    await _deliveryRepo.InsertDeliveryItem(
                        conn, tx,
                        deliveryId,
                        p.ProductId,
                        p.Name,
                        p.Price,
                        p.Quantity,
                        p.WeightGrams,
                        totalLine
                    );
                }

                // 7. Зберегти snapshot адреси
                await _deliveryRepo.InsertAddressSnapshot(conn, tx, deliveryId, address);

                result = deliveryId;
            });

            return result;
        }

        public async Task<List<DeliveryUserResult>> GetDeliveriesByUserIdAsync(int userId)
        {
            var result = new List<DeliveryUserResult>();
            await _tx.ExecuteAsync(async (conn, tx) =>
            {
                result = await _deliveryRepo.GetDeliveriesByUserId(conn, tx, userId);
            });

            return result;
        }

        public async Task AcceptDeliveryByRestaurantAsync(int userId, int deliveryId)
        {
            await _tx.ExecuteAsync(async (conn, tx) =>
            {
                int? restaurantId = await _productRepo.CheckUserIdInRestaurant(conn, tx, userId);

                var delivery = await _deliveryRepo.GetRestaurantIdStatusByDeliveryId(conn, tx, deliveryId);

                if (delivery == null)
                    throw new BusinessException("NOT_FOUND", "Delivery not found");

                if (restaurantId == null)
                    throw new UnauthorizedException("UserId claim missing");

                if (delivery.RestaurantId != restaurantId)
                    throw new ForbiddenException("You cannot manage this delivery");

                if (delivery.Status != (int)DeliveryStatus.Created)
                    throw new BusinessException("INVALID_STATUS", "Delivery cannot be confirmed");

                await _deliveryRepo.UpdateStatus(conn, tx, deliveryId,  DeliveryStatus.RestaurantConfirmed);

            });
        }

        public async Task CancelDeliveryByRestaurantAsync(int userId, int deliveryId)
        {
            await _tx.ExecuteAsync(async (conn, tx) =>
            {
                int? restaurantId = await _productRepo.CheckUserIdInRestaurant(conn, tx, userId);

                var delivery = await _deliveryRepo.GetRestaurantIdStatusByDeliveryId(conn, tx, deliveryId);

                if (delivery == null)
                    throw new BusinessException("NOT_FOUND", "Delivery not found");

                if (restaurantId == null)
                    throw new UnauthorizedException("UserId claim missing");

                if (delivery.RestaurantId != restaurantId)
                    throw new ForbiddenException("You cannot manage this delivery");

                if (delivery.Status != (int)DeliveryStatus.Created)
                    throw new BusinessException("INVALID_STATUS", "Delivery cannot be confirmed");

                await _deliveryRepo.UpdateStatus(conn, tx, deliveryId, DeliveryStatus.Canceled);

            });
        }

        public async Task<List<DeliveryUserResult>> GetDeliveriesByRestaurantUserAsync(int page, int pageSize, DeliveryStatus? status, int userId)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 50) pageSize = 50;

            int offset = (page - 1) * pageSize;

            List<DeliveryUserResult> deliveries = new List<DeliveryUserResult>();

            await _tx.ExecuteAsync(async (conn, tx) =>
            {
                int? restaurantId = await _productRepo.CheckUserIdInRestaurant(conn, tx, userId);

                if (restaurantId == null)
                    throw new UnauthorizedException("UserId claim missing");

                deliveries = await _deliveryRepo.GetDeliveries(conn, tx, offset, pageSize, status, restaurantId.Value);
            });

            return deliveries;
        }

        public async Task<List<DeliveryUserResult>> GetDeliveriesByCourierAsync(int page, int pageSize, DeliveryStatus deliveryStatus)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 50) pageSize = 50;

            int offset = (page - 1) * pageSize;

            List<DeliveryUserResult> deliveries = new List<DeliveryUserResult>();

            await _tx.ExecuteAsync(async (conn, tx) =>
            {
                deliveries = await _deliveryRepo.GetDeliveries(conn, tx, offset, pageSize, deliveryStatus);
            });

            return deliveries;

        }

        public async Task AcceptDeliveryByCourierAsync(int deliveryId, int userId  )
        {
            await _tx.ExecuteAsync(async (conn, tx) =>
            {
                if(await _deliveryRepo.AcceptedDeliveryByCourier(conn, tx, deliveryId, userId, DeliveryStatus.CourierAssigned, DeliveryStatus.RestaurantConfirmed) == 0)
                {
                    throw new BusinessException("DELIVERY_ALREADY_TAKEN", "Delivery already taken");
                }

                await _deliveryRepo.InsertDeliveryAssignments(conn, tx, deliveryId, userId);
            });
        }

        public async Task PickedUpDeliveryByCourierAsync(int deliveryId, int userId)
        {
            await _tx.ExecuteAsync(async (conn, tx) =>
            {
                int rows = await _deliveryRepo.PickupDelivery(
                    conn, tx,
                    deliveryId,
                    userId,
                    DeliveryStatus.PickedUp,
                    DeliveryStatus.CourierAssigned);

                if (rows == 0)
                    throw new BusinessException("DELIVERY_STATUS_NOT_CHANGED",
                        "Delivery cannot be picked up");
            });
        }

        public async Task ConfirmationsDeliveryByCourierAsync(int deliveryId, int courierId)
        {
            await _tx.ExecuteAsync(async (conn, tx) =>
            {
                int rows = await _deliveryRepo.InsertDeliveryConfirmations(
                    conn, tx,
                    deliveryId,
                    courierId,
                    ConfirmationRole.Courier);

                if (rows == 0)
                    throw new BusinessException("ALREADY_CONFIRMED", "Alredy confirmed");

                
                await _deliveryRepo.UpdateDeliveryStatus(
                    conn, tx,
                    deliveryId,
                    DeliveryStatus.Delivered);
                
            });
        }

        public async Task AcceptDeliveryByUserAsync(int deliveryId, int userId)
        {
            await _tx.ExecuteAsync(async (conn, tx) =>
            {
                int rows = await _deliveryRepo.InsertDeliveryConfirmationsUser(
                    conn, tx,
                    deliveryId,
                    userId,
                    ConfirmationRole.Courier);

                if (rows == 0)
                    throw new BusinessException("ERROR", "Error");


                rows = await _deliveryRepo.UpdateDeliveryStatus(
                    conn, tx,
                    deliveryId,
                    DeliveryStatus.Delivered,
                    DeliveryStatus.PickedUp);

                if (rows == 0)
                    throw new BusinessException("INVALID_STATUS", "Invalid status");
            });
        }
        private decimal CalculateDeliveryFee(int weightGrams)
        {
            if (weightGrams <= 2500) return 79;
            if (weightGrams <= 5000) return 99;
            if (weightGrams <= 10000) return 129;

            throw new BusinessException("TOO_HEAVY", "Maximum weight is 10kg");
        }

        
    }
}
