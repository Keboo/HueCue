using Emgu.CV;
using Emgu.CV.CvEnum;
using OBSWebsocketDotNet;

namespace HueCue.Sources;

public sealed class ObsStreamSource : IStreamSource
{
    private readonly OBSWebsocket _obs;
    private readonly string _host;
    private readonly string _port;
    private readonly string _password;
    private bool _disposed;
    private bool _isConnected;

    public double Fps { get; } = 30;
    public string Name { get; } = "OBS Studio Stream";

    public ObsStreamSource(string host = "localhost", string port = "4455", string password = "")
    {
        _host = host;
        _port = port;
        _password = password;
        _obs = new OBSWebsocket();
        
        // Set up event handlers
        _obs.Connected += OnConnected;
        _obs.Disconnected += OnDisconnected;
    }

    private void OnConnected(object? sender, EventArgs e)
    {
        _isConnected = true;
        System.Diagnostics.Debug.WriteLine("Connected to OBS WebSocket");
    }

    private void OnDisconnected(object? sender, OBSWebsocketDotNet.Communication.ObsDisconnectionInfo e)
    {
        _isConnected = false;
        System.Diagnostics.Debug.WriteLine($"Disconnected from OBS WebSocket");
    }

    public async Task<bool> LoadAsync()
    {
        if (_isConnected)
            return true;

        try
        {
            var url = $"ws://{_host}:{_port}";
            System.Diagnostics.Debug.WriteLine($"Connecting to OBS at {url}...");
            
            // Connect to OBS WebSocket - use the deprecated Connect method if ConnectAsync doesn't exist
            #pragma warning disable CS0618 // Type or member is obsolete
            _obs.Connect(url, _password);
            #pragma warning restore CS0618 // Type or member is obsolete
            
            // Wait a bit for connection to establish
            var timeout = DateTime.Now.AddSeconds(5);
            while (!_isConnected && DateTime.Now < timeout)
            {
                await Task.Delay(100);
            }

            return _isConnected;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to connect to OBS: {ex.Message}");
            return false;
        }
    }

    public async Task<Mat?> GetFrameAsync()
    {
        if (_disposed || !_isConnected)
            return null;

        try
        {
            // Get a screenshot from the current program scene
            var currentScene = _obs.GetCurrentProgramScene();
            var currentSceneName = currentScene?.Name;

            if (string.IsNullOrEmpty(currentSceneName))
                return null;

            // Get a screenshot from OBS
            var screenshot = await Task.Run(() => _obs.GetSourceScreenshot(
                sourceName: currentSceneName,
                imageFormat: "jpg",
                imageWidth: 1920,
                imageHeight: 1080,
                imageCompressionQuality: 85));

            if (string.IsNullOrEmpty(screenshot))
                return null;

            // Remove the data URI prefix (e.g., "data:image/jpeg;base64,")
            var base64Data = screenshot;
            if (base64Data.Contains(','))
            {
                base64Data = base64Data.Split(',')[1];
            }

            // Convert base64 to byte array
            var imageData = Convert.FromBase64String(base64Data);

            // Convert byte array to Mat
            using Mat mat = new();
            CvInvoke.Imdecode(imageData, ImreadModes.AnyColor, mat);

            // Clone the mat to ensure it's independent
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
            System.Diagnostics.Debug.WriteLine($"Error getting frame from OBS: {ex.Message}");
            return null;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _obs.Connected -= OnConnected;
            _obs.Disconnected -= OnDisconnected;
            
            if (_isConnected)
            {
                _obs.Disconnect();
            }
            
            _disposed = true;
        }
    }
}
