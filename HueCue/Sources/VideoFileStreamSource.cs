using System.IO;

using Emgu.CV;

namespace HueCue.Sources;

public sealed class VideoFileStreamSource : IStreamSource
{
    private readonly VideoCapture _videoCapture;

    public double Fps { get; }
    public string Name { get; }

    public VideoFileStreamSource(string filePath)
    {
        _videoCapture = new VideoCapture(filePath);
        Fps = _videoCapture.Get(Emgu.CV.CvEnum.CapProp.Fps);
        Name = Path.GetFileName(filePath);
    }

    public Task<Mat?> GetFrameAsync()
    {
        var currentFrame = new Mat();
        _videoCapture.Read(currentFrame);
        return Task.FromResult<Mat?>(currentFrame);
    }

    public void Dispose()
    {
        ((IDisposable)_videoCapture).Dispose();
    }
}
