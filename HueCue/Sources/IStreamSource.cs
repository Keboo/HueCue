using Emgu.CV;

namespace HueCue;

public interface IStreamSource : IDisposable
{
    string Name { get; }
    double Fps { get; }
    Task<bool> LoadAsync() { return Task.FromResult(true); }
    Task<Mat?> GetFrameAsync();
}
