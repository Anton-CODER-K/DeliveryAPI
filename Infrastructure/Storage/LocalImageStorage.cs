using DeliveryAPI.Application.Exeptions;
using DeliveryAPI.Application.Interfaces;
using DeliveryAPI.Application.Models.Result;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;



namespace DeliveryAPI.Infrastructure.Storage
{
    public class LocalImageStorage : IImageStorage
    {
        private readonly string _basePath;

        public LocalImageStorage(string basePath)
        {
            _basePath = basePath;
        }

        public async Task<ImageVariants> SaveImageAsync(Stream stream, string folder)
        {
            var id = Guid.NewGuid().ToString();

            var basePath = Path.Combine(_basePath, folder, id);

            Directory.CreateDirectory(basePath);

            using var image = await Image.LoadAsync(stream);

            var originalPath = Path.Combine(basePath, "original.jpg");
            await image.SaveAsJpegAsync(originalPath, new JpegEncoder
            {
                Quality = 85
            });

            using var medium = image.Clone(x => x.Resize(new ResizeOptions
            {
                Size = new Size(600, 600),
                Mode = ResizeMode.Max
            }));
            var mediumPath = Path.Combine(basePath, "medium.jpg");
            await medium.SaveAsync(mediumPath);

            using var thumb = image.Clone(x => x.Resize(new ResizeOptions
            {
                Size = new Size(150, 150),
                Mode = ResizeMode.Crop
            }));
            var thumbPath = Path.Combine(basePath, "thumb.jpg");
            await thumb.SaveAsync(thumbPath);

            return new ImageVariants
            {
                Folder = $"{folder}/{id}",
                Original = $"/images/{folder}/{id}/original.jpg",
                Medium = $"/images/{folder}/{id}/medium.jpg",
                Thumb = $"/images/{folder}/{id}/thumb.jpg"
            };
        }

        public Task DeleteImageAsync(string folderPath)
        {
            var fullPath = Path.Combine(_basePath, folderPath);

            if (Directory.Exists(fullPath))
                Directory.Delete(fullPath, true);

            return Task.CompletedTask;
        }


        public static bool IsImage(IFormFile image)
        {
            if (image == null || image.Length == 0)
                throw new BusinessException("EMPTY", "Image is empty");

            if (image.Length > 5 * 1024 * 1024)
                throw new BusinessException("TOO LARGE", "Max size 5MB");

            if (!image.ContentType.StartsWith("image/"))
                throw new BusinessException("INVALID TYPE", "Only images allowed");

            return true;
        }
    }
}
