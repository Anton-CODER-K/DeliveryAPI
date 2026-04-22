using DeliveryAPI.Application.Models.Result;

namespace DeliveryAPI.Application.Interfaces
{
    public interface IImageStorage
    {
        Task<ImageVariants> SaveImageAsync(Stream stream, string folder);
        Task DeleteImageAsync(string filePath);
    }
}
