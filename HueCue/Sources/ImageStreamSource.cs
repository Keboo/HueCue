using System.IO;

using Emgu.CV;

namespace HueCue.Sources;

public sealed class ImageStreamSource(string filePath) : IStreamSource
{
    public string Name { get; } = Path.GetFileName(filePath);
    public double Fps { get; } = 0;

    public Task<Mat?> GetFrameAsync() 
        => Task.FromResult<Mat?>(new Mat(filePath));

    public void Dispose() { }
}
