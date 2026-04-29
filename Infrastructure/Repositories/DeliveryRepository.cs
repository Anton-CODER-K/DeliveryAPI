using DeliveryAPI.Api.Contracts.Response;
using DeliveryAPI.Application.Enums;
using DeliveryAPI.Application.Exeptions;
using DeliveryAPI.Application.Models.Result;
using DeliveryAPI.Common;
using DeliveryAPI.Infrastructure.Entity.ReadModel;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Npgsql;
using NpgsqlTypes;
using System.Drawing;
using System.Net.NetworkInformation;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Xml.Linq;

namespace DeliveryAPI.Infrastructure.Repositories
{
    public class DeliveryRepository
    {
        public async Task<RestaurantReadModel?> GetRestaurant(NpgsqlConnection conn, NpgsqlTransaction tx, int restaurantId)
        {
            const string sql = """
                Select commission_percent
                From restaurants
                Where restaurant_id = @restaurantId
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.Add("@restaurantId", NpgsqlDbType.Integer).Value = restaurantId;

            await using var reader = await cmd.ExecuteReaderAsync();

            if (reader.Read())
            {
                return new RestaurantReadModel
                {
                    RestaurantId = restaurantId,
                    CommissionPercent = reader.GetDecimal(0)
                };
            }

            return null;
        }

        public async Task<int> InsertDelivery(NpgsqlConnection conn, NpgsqlTransaction tx, int userId, int restaurantId, decimal subtotal, decimal deliveryFee, decimal commissionPercent, decimal commissionAmount, decimal totalAmount, int totalWeight, int paymentMethod, string description)
        {
            const string sql = """
                Insert Into delivery (user_id, restaurant_id, subtotal_amount, delivery_fee, commission_percent, commission_amount, total_price, total_weight_grams, payment_method_id, status_delivery_id, description)
                Values (@userId, @restaurantId, @subtotal, @deliveryFee, @commissionPercent, @commissionAmount, @totalAmount, @totalWeight, @paymentMethod, 0, @description)
                Returning delivery_id
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.Add("@userId", NpgsqlDbType.Integer).Value = userId;
            cmd.Parameters.Add("@restaurantId", NpgsqlDbType.Integer).Value = restaurantId;
            cmd.Parameters.Add("@subtotal", NpgsqlDbType.Numeric).Value = subtotal;
            cmd.Parameters.Add("@deliveryFee", NpgsqlDbType.Numeric).Value = deliveryFee;
            cmd.Parameters.Add("@commissionPercent", NpgsqlDbType.Numeric).Value = commissionPercent;
            cmd.Parameters.Add("@commissionAmount", NpgsqlDbType.Numeric).Value = commissionAmount;
            cmd.Parameters.Add("@totalAmount", NpgsqlDbType.Numeric).Value = totalAmount;
            cmd.Parameters.Add("@totalWeight", NpgsqlDbType.Integer).Value = totalWeight;
            cmd.Parameters.Add("@paymentMethod", NpgsqlDbType.Integer).Value = paymentMethod;
            cmd.Parameters.Add("@description", NpgsqlDbType.Text).Value = description;

            var result = await cmd.ExecuteScalarAsync();
            if (result == null)
                throw new BusinessException("SESSION_CREATE_FAILED", "Failed to create session");

            return (int)result;


        }

        public async Task InsertDeliveryItem(NpgsqlConnection conn, NpgsqlTransaction tx, int deliveryId, int productId, string name, decimal price, int quantity, int weightGrams, decimal totalLine)
        {
            const string sql = """
                Insert Into delivery_items(delivery_id, product_name, product_price, quantity, product_weight_grams, product_id, total_line_amount)
                Values (@deliveryId, @name, @price, @quantity, @weightGrams, @productId ,@totalLine)
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.Add("@deliveryId", NpgsqlDbType.Integer).Value = deliveryId;
            cmd.Parameters.Add("@name", NpgsqlDbType.Varchar).Value = name;
            cmd.Parameters.Add("@price", NpgsqlDbType.Numeric).Value = price;
            cmd.Parameters.Add("@quantity", NpgsqlDbType.Integer).Value = quantity;
            cmd.Parameters.Add("@weightGrams", NpgsqlDbType.Integer).Value = weightGrams;
            cmd.Parameters.Add("@productId", NpgsqlDbType.Integer).Value = productId;
            cmd.Parameters.Add("@totalLine", NpgsqlDbType.Numeric).Value = totalLine;

            await cmd.ExecuteNonQueryAsync();


        }

        public async Task InsertAddressSnapshot(NpgsqlConnection conn, NpgsqlTransaction tx, int deliveryId, AddressReadModel address)
        {
            const string sql = """
                Insert Into delivery_address_snapshot(delivery_id, latitude, longitude, house, apartment, entrance, floor, comment)
                Values (@deliveryId, @latitude, @longitude, @house, @apartment, @entrance, @floor, @comment)
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.Add("@deliveryId", NpgsqlDbType.Integer).Value = deliveryId;
            cmd.Parameters.Add("@latitude", NpgsqlDbType.Numeric).Value = address.Latitude;
            cmd.Parameters.Add("@longitude", NpgsqlDbType.Numeric).Value = address.Longitude;
            cmd.Parameters.Add("@house", NpgsqlDbType.Varchar).Value = address.House;
            cmd.Parameters.Add("@apartment", NpgsqlDbType.Varchar).Value = (object?)address.Apartment ?? DBNull.Value;
            cmd.Parameters.Add("@entrance", NpgsqlDbType.Varchar).Value = (object?)address.Entrance ?? DBNull.Value;
            cmd.Parameters.Add("@floor", NpgsqlDbType.Varchar).Value = (object?)address.Floor ?? DBNull.Value;
            cmd.Parameters.Add("@comment", NpgsqlDbType.Text).Value = (object?)address.Comment ?? DBNull.Value;

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<DeliveryUserResult>> GetDeliveriesByUserId(NpgsqlConnection conn, NpgsqlTransaction tx, int userId)
        {
            var deliveries = new Dictionary<int, DeliveryUserResult>();

            const string sql = """
                Select
                    d.delivery_id,
                    sd.name,
                    pm.name,
                    ps.name,
                    d.total_price,
                    d.total_weight_grams,
                    d.created_at,
                    di.product_name,
                    di.quantity,
                    di.total_line_amount,
                    d.description
                From delivery d
                Join delivery_items di On di.delivery_id = d.delivery_id
                Join status_delivery sd On sd.status_delivery_id = d.status_delivery_id
                Join payment_method pm On pm.payment_method_id = d.payment_method_id
                Left Join payments p On p.delivery_id = d.delivery_id
                Left Join payment_statuses ps On ps.Id = p.status_id 
                Where user_id = @userId
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.Add("@userId", NpgsqlDbType.Integer).Value = userId;

            await using var reader = await cmd.ExecuteReaderAsync();

            while (reader.Read())
            {
                int deliveryId = reader.GetInt32(0);

                if (!deliveries.ContainsKey(deliveryId))
                {
                    deliveries[deliveryId] = new DeliveryUserResult
                    {
                        DeliveryId = deliveryId,
                        StatusDelivery = reader.GetString(1),
                        PaymentMethod = reader.GetString(2),
                        PaymentStatus = reader.IsDBNull(3) ? null : reader.GetString(3),
                        TotalPrice = reader.GetDecimal(4),
                        Total_weight_grams = reader.GetInt32(5),
                        CreatedAt = reader.GetDateTime(6),
                        Description = reader.GetString(10),
                        Items = new List<DeliveryUserItem>()
                    };
                }

                deliveries[deliveryId].Items.Add(new DeliveryUserItem
                {
                    ProductName = reader.GetString(7),
                    Quantity = reader.GetInt32(8),
                    TotalLineAmount = reader.GetDecimal(9)
                });

            }

            return deliveries.Values.ToList();

        }

        public async Task<RestaurantIdAndStatusResult?> GetRestaurantIdStatusByDeliveryId(NpgsqlConnection conn, NpgsqlTransaction tx, int deliveryId)
        {
            const string sql = """
                Select 
                    restaurant_id, status_delivery_id
                From
                    delivery
                Where
                    delivery_id = @deliveryId
                Limit 1

                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.Add("@deliveryId", NpgsqlDbType.Integer).Value = deliveryId;

            await using var reader = await cmd.ExecuteReaderAsync();

            if (reader.Read())
            {
                return new RestaurantIdAndStatusResult
                {
                    RestaurantId = reader.GetInt32(0),
                    Status = reader.GetInt32(1)
                };
            }

            return null;

        }

        public async Task<RestaurantIdAndStatusPaymentResult?> GetRestaurantIdStatusPaymentByDeliveryId(NpgsqlConnection conn, NpgsqlTransaction tx, int deliveryId)
        {
            const string sql = """
                Select 
                    d.restaurant_id, d.status_delivery_id, p.status_id, d.payment_method_id 
                From
                    delivery d
                Join
                    payments p On p.delivery_id = d.delivery_id
                Where
                    d.delivery_id = @deliveryId
                Limit 1

                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.Add("@deliveryId", NpgsqlDbType.Integer).Value = deliveryId;

            await using var reader = await cmd.ExecuteReaderAsync();

            if (reader.Read())
            {
                return new RestaurantIdAndStatusPaymentResult
                {
                    RestaurantId = reader.GetInt32(0),
                    Status = reader.GetInt32(1),
                    StatusPayment = reader.GetInt32(2),
                    PaymentMethod = reader.GetInt32(3),
                };
            }

            return null;

        }

        public async Task UpdateStatus(NpgsqlConnection conn, NpgsqlTransaction tx, int deliveryId, DeliveryStatus restaurantConfirmed)
        {
            const string sql = """
                Update delivery
                set status_delivery_id = @restaurantConfirmed
                Where delivery_id = @deliveryId
                """;
            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.Add("@restaurantConfirmed", NpgsqlDbType.Integer).Value = (int)restaurantConfirmed;
            cmd.Parameters.Add("@deliveryId", NpgsqlDbType.Integer).Value = deliveryId;

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<DeliveryUserResult>> GetDeliveries(NpgsqlConnection conn, NpgsqlTransaction tx, int offset, int pageSize, DeliveryStatus? status, int restaurantId)
        {
            var deliveries = new Dictionary<int, DeliveryUserResult>();


            const string sql = """
                Select
                    d.delivery_id,
                    sd.name,
                    pm.name,
                    ps.name,
                    d.total_price,
                    d.total_weight_grams,
                    d.created_at,
                    di.product_name,
                    di.quantity,
                    di.total_line_amount,
                    d.description
                From delivery d
                Join delivery_items di On di.delivery_id = d.delivery_id
                Join status_delivery sd On sd.status_delivery_id = d.status_delivery_id
                Join payment_method pm On pm.payment_method_id = d.payment_method_id
                Left Join payments p On p.delivery_id = d.delivery_id
                Left Join payment_statuses ps On ps.Id = p.status_id 
                WHERE (@status IS NULL OR d.status_delivery_id = @status)
                AND d.restaurant_id = @restaurantId
                ORDER BY created_at DESC
                LIMIT @limit OFFSET @offset;
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.Add("@limit", NpgsqlDbType.Integer).Value = pageSize;
            cmd.Parameters.Add("@offset", NpgsqlDbType.Integer).Value = offset;
            cmd.Parameters.Add("@status", NpgsqlDbType.Integer).Value = status is null ? DBNull.Value : (int)status;
            cmd.Parameters.Add("@restaurantId", NpgsqlDbType.Integer).Value = restaurantId;


            await using var reader = await cmd.ExecuteReaderAsync();

            while (reader.Read())
            {
                int deliveryId = reader.GetInt32(0);

                if (!deliveries.ContainsKey(deliveryId))
                {
                    deliveries[deliveryId] = new DeliveryUserResult
                    {
                        DeliveryId = deliveryId,
                        StatusDelivery = reader.GetString(1),
                        PaymentMethod = reader.GetString(2),
                        PaymentStatus = reader.IsDBNull(3) ? null : reader.GetString(3),
                        TotalPrice = reader.GetDecimal(4),
                        Total_weight_grams = reader.GetInt32(5),
                        CreatedAt = reader.GetDateTime(6),
                        Description = reader.IsDBNull(10) ? null : reader.GetString(10),
                        Items = new List<DeliveryUserItem>()
                    };
                }

                deliveries[deliveryId].Items.Add(new DeliveryUserItem
                {
                    ProductName = reader.GetString(7),
                    Quantity = reader.GetInt32(8),
                    TotalLineAmount = reader.GetDecimal(9)
                });

            }

            return deliveries.Values.ToList();
        }

        public async Task<List<DeliveryUserByCourierResult?>> GetDeliveriesByCourier(NpgsqlConnection conn, NpgsqlTransaction tx, int offset, int pageSize, DeliveryStatus? status, int restaurantId)
        {
            var deliveries = new Dictionary<int, DeliveryUserByCourierResult>();


            const string sql = """
                Select
                    d.delivery_id,
                    sd.name,
                    pm.name,
                    ps.name,

                    u.name,
                    u.phone_number,
                    u.birth_date,

                    d.total_price,
                    d.total_weight_grams,
                    d.created_at,
                    di.product_name,
                    di.quantity,
                    di.total_line_amount
                From delivery d
                Join delivery_items di On di.delivery_id = d.delivery_id
                Join status_delivery sd On sd.status_delivery_id = d.status_delivery_id
                Join payment_method pm On pm.payment_method_id = d.payment_method_id
                Left Join payments p On p.delivery_id = d.delivery_id
                Left Join payment_statuses ps On ps.Id = p.status_id 
                Join users u On u.user_id = d.user_id
                WHERE (@status IS NULL OR d.status_delivery_id = @status)
                AND d.restaurant_id = @restaurantId
                ORDER BY created_at DESC
                LIMIT @limit OFFSET @offset;
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.Add("@limit", NpgsqlDbType.Integer).Value = pageSize;
            cmd.Parameters.Add("@offset", NpgsqlDbType.Integer).Value = offset;
            cmd.Parameters.Add("@status", NpgsqlDbType.Integer).Value = status is null ? DBNull.Value : (int)status;
            cmd.Parameters.Add("@restaurantId", NpgsqlDbType.Integer).Value = restaurantId;


            await using var reader = await cmd.ExecuteReaderAsync();

            while (reader.Read())
            {
                int deliveryId = reader.GetInt32(0);

                if (!deliveries.ContainsKey(deliveryId))
                {
                    deliveries[deliveryId] = new DeliveryUserByCourierResult
                    {
                        DeliveryId = deliveryId,
                        StatusDelivery = reader.GetString(1),
                        PaymentMethod = reader.GetString(2),
                        PaymentStatus = reader.IsDBNull(3) ? null : reader.GetString(3),
                        UserName = reader.GetString(4),
                        UserPhone = reader.GetString(5),
                        UserBirthday = reader.GetDateTime(6),
                        TotalPrice = reader.GetDecimal(7),
                        Total_weight_grams = reader.GetInt32(8),
                        CreatedAt = reader.GetDateTime(9),
                        Items = new List<DeliveryUserByCourierItem>()
                    };
                }

                deliveries[deliveryId].Items.Add(new DeliveryUserByCourierItem
                {
                    ProductName = reader.GetString(10),
                    Quantity = reader.GetInt32(11),
                    TotalLineAmount = reader.GetDecimal(12)
                });

            }

            return deliveries.Values.ToList();
        }




        public async Task<List<DeliveryUserResult>> GetDeliveries(NpgsqlConnection conn, NpgsqlTransaction tx, int offset, int pageSize, DeliveryStatus? status)
        {
            var deliveries = new Dictionary<int, DeliveryUserResult>();


            const string sql = """
                SELECT 
                    d.delivery_id,
                    sd.name,
                    pm.name,
                    d.total_price,
                    d.total_weight_grams,
                    d.created_at,
                    di.product_name,
                    di.quantity,
                    di.total_line_amount,
                    d.description
                FROM delivery d
                Join delivery_items di On di.delivery_id = d.delivery_id
                Join status_delivery sd On sd.status_delivery_id = d.status_delivery_id
                Join payment_method pm On pm.payment_method_id = d.payment_method_id
                WHERE (@status IS NULL OR d.status_delivery_id = @status)
                ORDER BY created_at DESC
                LIMIT @limit OFFSET @offset;
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.Add("@limit", NpgsqlDbType.Integer).Value = pageSize;
            cmd.Parameters.Add("@offset", NpgsqlDbType.Integer).Value = offset;
            cmd.Parameters.Add("@status", NpgsqlDbType.Integer).Value = status is null ? DBNull.Value : (int)status;


            await using var reader = await cmd.ExecuteReaderAsync();

            while (reader.Read())
            {
                int deliveryId = reader.GetInt32(0);

                if (!deliveries.ContainsKey(deliveryId))
                {
                    deliveries[deliveryId] = new DeliveryUserResult
                    {
                        DeliveryId = deliveryId,
                        StatusDelivery = reader.GetString(1),
                        PaymentMethod = reader.GetString(2),
                        TotalPrice = reader.GetDecimal(3),
                        Total_weight_grams = reader.GetInt32(4),
                        CreatedAt = reader.GetDateTime(5),
                        Description = reader.IsDBNull(9) ? null : reader.GetString(9),
                        Items = new List<DeliveryUserItem>()
                    };
                }

                deliveries[deliveryId].Items.Add(new DeliveryUserItem
                {
                    ProductName = reader.GetString(6),
                    Quantity = reader.GetInt32(7),
                    TotalLineAmount = reader.GetDecimal(8)
                });

            }

            return deliveries.Values.ToList();
        }

        public async Task<List<DeliveryAdminResult>> GetDeliveriesAdmin(NpgsqlConnection conn, NpgsqlTransaction tx, int offset, int pageSize, DeliveryStatus? status)
        {
            var deliveries = new Dictionary<int, DeliveryAdminResult>();


            const string sql = """
                WITH deliveries_page AS (
                    SELECT *
                    FROM delivery
                    WHERE (@status IS NULL OR status_delivery_id = @status)
                    ORDER BY created_at DESC
                    LIMIT @limit OFFSET @offset
                )

                SELECT
                    d.delivery_id,
                    d.user_id,
                    d.courier_user_id,
                    d.restaurant_id,
                    sd.name,
                    ps.name,
                    pm.name,
                    d.total_price,
                    d.subtotal_amount,
                    d.delivery_fee,
                    d.commission_amount,
                    d.commission_percent,
                    d.total_weight_grams,
                    d.created_at,
                    d.description,
                    di.product_name,
                    di.quantity,
                    di.total_line_amount
                FROM deliveries_page d
                JOIN delivery_items di ON di.delivery_id = d.delivery_id
                JOIN status_delivery sd ON sd.status_delivery_id = d.status_delivery_id
                JOIN payment_method pm ON pm.payment_method_id = d.payment_method_id
                Left Join payments p On p.delivery_id = d.delivery_id
                Left Join payment_statuses ps On ps.Id = p.status_id 
                ORDER BY d.created_at DESC;
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.Add("@limit", NpgsqlDbType.Integer).Value = pageSize;
            cmd.Parameters.Add("@offset", NpgsqlDbType.Integer).Value = offset;
            cmd.Parameters.Add("@status", NpgsqlDbType.Integer).Value = status is null ? DBNull.Value : (int)status;


            await using var reader = await cmd.ExecuteReaderAsync();

            while (reader.Read())
            {
                int deliveryId = reader.GetInt32(0);

                if (!deliveries.ContainsKey(deliveryId))
                {
                    deliveries[deliveryId] = new DeliveryAdminResult
                    {
                        DeliveryId = deliveryId,
                        UserId = reader.GetInt32(1),
                        CourierId = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                        RestaurantId = reader.GetInt32(3),
                        StatusDelivery = reader.GetString(4),
                        StatusPayment = reader.IsDBNull(5) ? null : reader.GetString(5),
                        PaymentMethod = reader.IsDBNull(6) ? null : reader.GetString(6),
                        TotalPrice = reader.GetDecimal(7),
                        ProductPrice = reader.GetDecimal(8),
                        DeliveryFee = reader.GetDecimal(9),
                        CommissionsAmount = reader.GetDecimal(10),
                        CommissionPercent = reader.GetDecimal(11),
                        Total_weight_grams = reader.GetInt32(12),
                        CreatedAt = reader.GetDateTime(13),
                        Description = reader.IsDBNull(14) ? null : reader.GetString(14),
                        Items = new List<DeliveryUserItem>()
                    };
                }

                deliveries[deliveryId].Items.Add(new DeliveryUserItem
                {
                    ProductName = reader.GetString(15),
                    Quantity = reader.GetInt32(16),
                    TotalLineAmount = reader.GetDecimal(17)
                });

            }

            return deliveries.Values.ToList();
        }

        public async Task<int> AcceptedDeliveryByCourier(NpgsqlConnection conn, NpgsqlTransaction tx, int deliveryId, int courierId)
        {
            const string sql = """
                UPDATE delivery
                SET
                    courier_user_id = @courierId
                WHERE delivery_id = @deliveryId
                    AND user_id != @courierId
                    AND courier_user_id IS NULL
                    AND status_delivery_id IN (@preparing, @readyForPickup)
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            
            cmd.Parameters.Add("@courierId", NpgsqlDbType.Integer).Value = courierId;
            cmd.Parameters.Add("@deliveryId", NpgsqlDbType.Integer).Value = deliveryId;
           
            //cmd.Parameters.Add("@courierAssigned", NpgsqlDbType.Integer).Value = (int)DeliveryStatus.ReadyForPickup;
            cmd.Parameters.Add("@preparing", NpgsqlDbType.Integer).Value = (int)DeliveryStatus.Preparing;
            cmd.Parameters.Add("@readyForPickup", NpgsqlDbType.Integer).Value = (int)DeliveryStatus.ReadyForPickup;

            int rows = await cmd.ExecuteNonQueryAsync();

            if (rows == 0)
                throw new BusinessException("DELIVERY_NOT_TAKEN", "Delivery already taken but order this you");

            return rows;

        }

        public async Task<int> InsertDeliveryAssignments(NpgsqlConnection conn, NpgsqlTransaction tx, int deliveryId, int courierId)
        {
            const string sql = """
                Insert Into delivery_assignments (delivery_id, courier_user_id)
                Values (@deliveryId, @courierId)
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.Add("@deliveryId", NpgsqlDbType.Integer).Value = deliveryId;
            cmd.Parameters.Add("@courierId", NpgsqlDbType.Integer).Value = courierId;

            int rows = await cmd.ExecuteNonQueryAsync();

            if (rows == 0)
                throw new BusinessException("DELIVERY_ALREADY_TAKEN", "Delivery already taken");

            return rows;
        }

        public async Task CheckStatusDelivery(NpgsqlConnection conn, NpgsqlTransaction tx, int deliveryId, DeliveryStatus courierAssigned)
        {
            const string sql = """
                Select status_delivery_id
                From delivery
                Where delivery_id = @deliveryId
                Limit 1
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.Add("@deliveryId", NpgsqlDbType.Integer).Value = deliveryId;
           
            int result = (int)await cmd.ExecuteScalarAsync();

            if (result != (int)courierAssigned)
                throw new BusinessException("DELIVERY_STATUS_NOT_CHANGED", "Delivery status has been no changed");


        }

        public async Task<bool> CheckCourierByDelivery(NpgsqlConnection conn, NpgsqlTransaction tx, int deliveryId, int userId)
        {
            const string sql = """
                Select courier_user_id
                From delivery
                Where delivery_id = @deliveryId
                Limit 1
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.Add("@deliveryId", NpgsqlDbType.Integer).Value = deliveryId;

            var result = await cmd.ExecuteScalarAsync();

            return result != null && (int)result == userId;


        }

        public async Task UpdateStatusDelivery(NpgsqlConnection conn, NpgsqlTransaction tx, int deliveryId, int userId, DeliveryStatus pickedUp)
        {
            const string sql = """
                Update delivery
                Set status_delivery_id = @pickedUp
                Where delivery_id = @deliveryId
                And courier_user_id = @userId
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.Add("@pickedUp", NpgsqlDbType.Integer).Value = (int)pickedUp;
            cmd.Parameters.Add("@deliveryId", NpgsqlDbType.Integer).Value = deliveryId;
            cmd.Parameters.Add("@userId", NpgsqlDbType.Integer).Value = userId;

            int rows = await cmd.ExecuteNonQueryAsync();
            if (rows == 0)
            {
                throw new BusinessException("DELIVERY_STATUS_NOT_CHANGED", "Delivery status has been no changed");
            }


        }

        public async Task<int> PickupDelivery(
            NpgsqlConnection conn,
            NpgsqlTransaction tx,
            int deliveryId,
            int userId,
            DeliveryStatus pickedUp,
            DeliveryStatus readyForPickup)
        {
            const string sql = """
                UPDATE delivery
                SET status_delivery_id = @pickedUp
                WHERE delivery_id = @deliveryId
                AND courier_user_id = @userId
                AND status_delivery_id = @courierAssigned;
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.Add("@pickedUp", NpgsqlDbType.Integer).Value = (int)pickedUp;
            cmd.Parameters.Add("@deliveryId", NpgsqlDbType.Integer).Value = deliveryId;
            cmd.Parameters.Add("@userId", NpgsqlDbType.Integer).Value = userId;
            cmd.Parameters.Add("@courierAssigned", NpgsqlDbType.Integer).Value = (int)readyForPickup;

            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<int> InsertDeliveryConfirmations(
             NpgsqlConnection conn,
             NpgsqlTransaction tx,
             int deliveryId,
             int courierId,
             ConfirmationRole role)
        {
            const string sql = """
                INSERT INTO delivery_confirmations (delivery_id, user_id, role_name_snapshot)
                SELECT d.delivery_id, @courierId, @role 
                FROM delivery d
                WHERE d.delivery_id = @deliveryId
                  AND d.courier_user_id = @courierId
                ON CONFLICT (delivery_id, user_id)
                DO NOTHING;
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.Add("@deliveryId", NpgsqlDbType.Integer).Value = deliveryId;
            cmd.Parameters.Add("@courierId", NpgsqlDbType.Integer).Value = courierId;
            cmd.Parameters.Add("@role", NpgsqlDbType.Varchar).Value = role.ToString();

            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<int> CountConfirmations(
            NpgsqlConnection conn,
            NpgsqlTransaction tx,
            int deliveryId)
        {
            const string sql = """
                SELECT COUNT(DISTINCT role_name_snapshot)
                FROM delivery_confirmations
                WHERE delivery_id = @deliveryId
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.AddWithValue("@deliveryId", deliveryId);

            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        public async Task UpdateDeliveryStatus(
            NpgsqlConnection conn,
            NpgsqlTransaction tx,
            int deliveryId,
            DeliveryStatus status)
        {
            const string sql = """
                UPDATE delivery
                SET status_delivery_id = @status
                WHERE delivery_id = @deliveryId
                AND (
                    SELECT COUNT(DISTINCT role_name_snapshot)
                    FROM delivery_confirmations
                    WHERE delivery_id = @deliveryId
                ) >= 2;
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.Add("@status", NpgsqlDbType.Integer)
                .Value = (int)status;

            cmd.Parameters.Add("@deliveryId", NpgsqlDbType.Integer)
                .Value = deliveryId;

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<int> UpdateDeliveryStatus(
            NpgsqlConnection conn,
            NpgsqlTransaction tx,
            int deliveryId,
            DeliveryStatus newStatus,
            DeliveryStatus expectedStatus)
        {
            const string sql = """
                UPDATE delivery
                SET status_delivery_id = @newStatus
                WHERE delivery_id = @deliveryId
                AND status_delivery_id = @expectedStatus;
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.Add("@newStatus", NpgsqlDbType.Integer)
                .Value = (int)newStatus;

            cmd.Parameters.Add("@expectedStatus", NpgsqlDbType.Integer)
                .Value = (int)expectedStatus;

            cmd.Parameters.Add("@deliveryId", NpgsqlDbType.Integer)
                .Value = deliveryId;

            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<int> InsertDeliveryConfirmationsUser(
             NpgsqlConnection conn,
             NpgsqlTransaction tx,
             int deliveryId,
             int userId,
             ConfirmationRole role)
        {
            const string sql = """
                INSERT INTO delivery_confirmations (delivery_id, user_id, role_name_snapshot)
                SELECT d.delivery_id, @userId, @role 
                FROM delivery d
                WHERE d.delivery_id = @deliveryId
                  AND d.user_id = @userId
                ON CONFLICT (delivery_id, user_id)
                DO NOTHING;
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.Add("@deliveryId", NpgsqlDbType.Integer).Value = deliveryId;
            cmd.Parameters.Add("@userId", NpgsqlDbType.Integer).Value = userId;
            cmd.Parameters.Add("@role", NpgsqlDbType.Varchar).Value = role.ToString();

            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<AddressDeliveryIdResponse?> GetDeliveryAddress(NpgsqlConnection conn, NpgsqlTransaction tx, int deliveryId)
        {
            const string sql = """
                Select latitude, longitude, house, apartment, entrance, floor, comment
                From delivery_address_snapshot
                Where delivery_id = @deliveryId
                Limit 1
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.Add("@deliveryId", NpgsqlDbType.Integer).Value = deliveryId;

            await using var reader = await cmd.ExecuteReaderAsync();

            if (reader.Read())
            {
                return new AddressDeliveryIdResponse
                {
                    latitude = reader.GetDecimal(0),
                    longitude = reader.GetDecimal(1),
                    house = reader.GetString(2),
                    apartment = reader.IsDBNull(3) ? null : reader.GetString(3),
                    entrance = reader.IsDBNull(4) ? null : reader.GetString(4),
                    floor = reader.IsDBNull(5) ? null : reader.GetString(5),
                    comment = reader.IsDBNull(6) ? null : reader.GetString(6)
                };
            }

            return null;
        }

        public async Task<List<DeliveryUserResult>> GetDeliveriesByCourierId(NpgsqlConnection conn, NpgsqlTransaction tx, int offset, int pageSize, int courierId, DeliveryStatus? status)
        {
            var deliveries = new Dictionary<int, DeliveryUserResult>();

            var sql = """
                SELECT 
                    d.delivery_id,
                    sd.name,
                    pm.name,
                    d.total_price,
                    d.total_weight_grams,
                    d.created_at,
                    di.product_name,
                    di.quantity,
                    di.total_line_amount
                FROM delivery d
                Join delivery_items di On di.delivery_id = d.delivery_id
                Join status_delivery sd On sd.status_delivery_id = d.status_delivery_id
                Join payment_method pm On pm.payment_method_id = d.payment_method_id
                WHERE courier_user_id = @courierId
                """;

            if (status != null)
            {
                sql += " AND d.status_delivery_id = @status";
            }

            sql += """   
             ORDER BY d.created_at DESC
             LIMIT @limit OFFSET @offset;
            """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.Add("@courierId", NpgsqlDbType.Integer).Value = courierId;
            cmd.Parameters.Add("@limit", NpgsqlDbType.Integer).Value = pageSize;
            cmd.Parameters.Add("@offset", NpgsqlDbType.Integer).Value = offset;

            if (status != null)
            {
                cmd.Parameters.Add("@status", NpgsqlDbType.Integer).Value = (int)status;
            }


            await using var reader = await cmd.ExecuteReaderAsync();

            while (reader.Read())
            {
                int deliveryId = reader.GetInt32(0);

                if (!deliveries.ContainsKey(deliveryId))
                {
                    deliveries[deliveryId] = new DeliveryUserResult
                    {
                        DeliveryId = deliveryId,
                        StatusDelivery = reader.GetString(1),
                        PaymentMethod = reader.GetString(2),
                        TotalPrice = reader.GetDecimal(3),
                        Total_weight_grams = reader.GetInt32(4),
                        CreatedAt = reader.GetDateTime(5),
                        Items = new List<DeliveryUserItem>()
                    };
                }

                deliveries[deliveryId].Items.Add(new DeliveryUserItem
                {
                    ProductName = reader.GetString(6),
                    Quantity = reader.GetInt32(7),
                    TotalLineAmount = reader.GetDecimal(8)
                });

            }

            return deliveries.Values.ToList();
        }

        public async Task<bool> CheckDeliveryByUserId(NpgsqlConnection conn, NpgsqlTransaction tx, int deliveryId, int userId)
        {
            const string sql = """
                Select 1
                From delivery
                Where delivery_id = @deliveryId
                and user_id = @userId
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.Add("@deliveryId", NpgsqlDbType.Integer).Value = deliveryId;
            cmd.Parameters.Add("@userId", NpgsqlDbType.Integer).Value = userId;

            var result = await cmd.ExecuteScalarAsync();

            return result != null;
        }

        public async Task<DeliveryPaymentResult> GetDeliveryPayment(NpgsqlConnection conn, NpgsqlTransaction tx, int deliveryId)
        {
            const string sql = """
                Select
                    d.user_id,
                    d.status_delivery_id,
                    d.total_price,
                    d.payment_method_id
                From delivery d
                Where delivery_id = @deliveryId
                Limit 1
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.Add("@deliveryId", NpgsqlDbType.Integer).Value = deliveryId;

            await using var reader = await cmd.ExecuteReaderAsync();

            if (reader.Read())
            {
                return new DeliveryPaymentResult
                {
                    UserId = reader.GetInt32(0),
                    Status = reader.GetInt32(1),
                    TotalPrice = reader.GetDecimal(2),
                    PaymentMethod = reader.GetInt32(3)
                };
            }

            throw new BusinessException("DELIVERY_NOT_FOUND", "Delivery not found");
        }

        public async Task<List<DeliveryCourierResult>> GetDeliveriesByCourier(NpgsqlConnection conn, NpgsqlTransaction tx,  int courierId, int offset, int pageSize, DeliveryStatus? status)
        {
            var deliveries = new Dictionary<int, DeliveryCourierResult>();


            const string sql = """
                WITH deliveries_page AS (
                    SELECT *
                    FROM delivery
                    WHERE (@status IS NULL OR status_delivery_id = @status)
                    ORDER BY created_at DESC
                    LIMIT @limit OFFSET @offset
                )

                SELECT
                    d.delivery_id,
                    d.restaurant_id,
                    d.status_delivery_id,
                    pm.name,
                    ps.name,
                    d.total_price,
                    d.total_weight_grams,
                    d.description,
                    d.created_at,

                    u.phone_number,
                    u.name,
                    i.folder,

                    das.latitude,
                    das.longitude,
                    das.house,
                    das.apartment,
                    das.entrance,
                    das.floor,
                    das.comment,

                    di.product_name,
                    di.quantity,
                    di.total_line_amount
                FROM deliveries_page d
                JOIN delivery_items di ON di.delivery_id = d.delivery_id
                JOIN payment_method pm ON pm.payment_method_id = d.payment_method_id
                JOIN users u ON u.user_id = d.user_id
                JOIN delivery_address_snapshot das ON das.delivery_id = d.delivery_id
                Left Join payments p On p.delivery_id = d.delivery_id
                Left Join payment_statuses ps On ps.Id = p.status_id 
                Left Join images i On i.image_id = u.image_id
                Where d.courier_user_id = @courierId
                ORDER BY d.created_at DESC;
                """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);

            cmd.Parameters.Add("@limit", NpgsqlDbType.Integer).Value = pageSize;
            cmd.Parameters.Add("@offset", NpgsqlDbType.Integer).Value = offset;
            cmd.Parameters.Add("@courierId", NpgsqlDbType.Integer).Value = courierId;
            cmd.Parameters.Add("@status", NpgsqlDbType.Integer).Value = status is null ? DBNull.Value : (int)status;


            await using var reader = await cmd.ExecuteReaderAsync();

            while (reader.Read())
            {
                int deliveryId = reader.GetInt32(0);

                if (!deliveries.ContainsKey(deliveryId))
                {
                    deliveries[deliveryId] = new DeliveryCourierResult
                    {
                        DeliveryId = deliveryId,
                        RestaurantId = reader.GetInt32(1),
                        StatusDelivery = reader.GetInt32(2),
                        PaymentMethod = reader.GetString(3),
                        PaymentStatus = reader.IsDBNull(4) ? null : reader.GetString(4),
                        TotalPrice = reader.GetDecimal(5),
                        Total_weight_grams = reader.GetInt32(6),
                        Description = reader.IsDBNull(7) ? null : reader.GetString(7),
                        CreatedAt = reader.GetDateTime(8),

                        User = new UserResult
                        {
                            Phone = reader.GetString(9),
                            Name = reader.GetString(10),
                            AvatarUrl = reader.IsDBNull(11) ? null : ($"{AppConfigURLBase.BaseUrl}" + "/images/" + reader.GetString(11) + "/thumb.jpg"),
                        },

                        Address = new AddressUserIdByCourierResponse
                        {
                            latitude = reader.GetDecimal(12),
                            longitude = reader.GetDecimal(13),
                            house = reader.GetString(14),
                            apartment = reader.IsDBNull(15) ? null : reader.GetString(15),
                            entrance = reader.IsDBNull(16) ? null : reader.GetString(16),
                            floor = reader.IsDBNull(17) ? null : reader.GetString(17),
                            comment = reader.IsDBNull(18) ? null : reader.GetString(18)
                        },
                        Items = new List<DeliveryCourierItem>()
                    };
                }

                deliveries[deliveryId].Items.Add(new DeliveryCourierItem
                {
                    ProductName = reader.GetString(19),
                    Quantity = reader.GetInt32(20),
                    TotalLineAmount = reader.GetDecimal(21)
                });

            }

            return deliveries.Values.ToList();
        }
    }
}
