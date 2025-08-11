using System.Threading.Tasks;

namespace Business.Services.ImageProcessing
{
    public interface IImageProcessingService
    {
        Task<byte[]> ResizeImageAsync(byte[] imageBytes, int maxWidth, int maxHeight, int quality = 85);
        Task<byte[]> ResizeImageIfNeededAsync(byte[] imageBytes);
        Task<(int width, int height)> GetImageDimensionsAsync(byte[] imageBytes);
        Task<bool> IsImageWithinLimitsAsync(byte[] imageBytes);
        Task<string> GetImageFormatAsync(byte[] imageBytes);
    }
}