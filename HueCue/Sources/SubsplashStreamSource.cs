using System.Net.Http;
using System.Text.RegularExpressions;

using Emgu.CV;

namespace HueCue.Sources;

public sealed class SubsplashStreamSource : IStreamSource
{
    private static readonly HttpClient _httpClient = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private VideoCapture? _videoCapture;

    public string Name { get; } = "Subsplash Stream Source";
    public double Fps { get; } = 30;

    public SubsplashStreamSource()
    {

    }

    public void Dispose()
    {
        _videoCapture?.Dispose();
        _videoCapture = null;
    }

    public async Task<Mat?> GetFrameAsync()
    {
        if (_videoCapture is null) return null;

        var currentFrame = new Mat();
        _videoCapture!.Read(currentFrame);
        return currentFrame;
    }

    public async Task<bool> LoadAsync()
    {
        if (_videoCapture is null)
        {
            var page = await _httpClient.GetStringAsync("https://subsplash.com/u/northviewbiblechurch/media/embed/d/*next-live?wmode=opaque");
            string pattern = @"\\""external_m3u8_url\\""\s*:\s*\\""([^""\\]+)\\""";
            Match match = Regex.Match(page, pattern);

            if (match.Success)
            {
                string m3u8Url = match.Groups[1].Value;
                _videoCapture = new VideoCapture(m3u8Url);
                return true;
            }
        }
        return false;
    }
}
