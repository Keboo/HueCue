using System.Net.Http;

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;

namespace HueCue;

public class AjaHeloStreamSource : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private bool _disposed;

    public AjaHeloStreamSource(string ipAddress = "192.168.10.248")
    {
        _baseUrl = $"http://{ipAddress}/wall/videofeed.jpg";
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(5); // 5 second timeout for network requests
    }

    public async Task<Mat?> GetFrameAsync()
    {
        if (_disposed)
            return null;

        try
        {
            // Generate random GUID for cache busting
            var cacheGuid = Guid.NewGuid().ToString();
            var url = $"{_baseUrl}?{cacheGuid}";

            // Download the image data
            using var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return null;

            var imageData = await response.Content.ReadAsByteArrayAsync();
            if (imageData.Length == 0)
                return null;

            // Convert byte array to Mat
            using Mat mat = new();
            CvInvoke.Imdecode(imageData, ImreadModes.AnyColor, mat);

            // Clone the mat to ensure it's independent of the vector, then dispose original
            if (mat?.IsEmpty == false)
            {
                var clonedMat = mat.Clone();
                mat.Dispose();
                return clonedMat;
            }

            mat?.Dispose();
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading frame from AJA Helo: {ex.Message}");
            return null;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient?.Dispose();
            _disposed = true;
        }
    }
}