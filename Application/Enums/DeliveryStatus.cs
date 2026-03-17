namespace DeliveryAPI.Application.Enums
{
    public enum DeliveryStatus
    {
        Created = 0,
        RestaurantConfirmed = 1,
        Paid = 2,
        Preparing = 3, 
        ReadyForPickup = 4,
        PickedUp = 5,
        Delivered = 6,
        Cancelled = 7,
    }
}
