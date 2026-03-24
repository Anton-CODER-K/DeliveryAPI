namespace DeliveryAPI.Application.Enums
{
    public enum DeliveryStatus
    {
        Created = 0,
        RestaurantConfirmed = 1,
        Preparing = 2, 
        ReadyForPickup = 3,
        PickedUp = 4,
        Delivered = 5,
        Cancelled = 6,
    }
}
