using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;

using System.ComponentModel;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Velopack;

namespace HueCue;

public partial class MainWindowViewModel : ObservableObject
{
    private VideoCapture? _videoCapture;
    private DispatcherTimer? _playbackTimer;
    private DispatcherTimer? _histogramTimer;
    private Mat? _currentFrame;

    [ObservableProperty]
    private ImageSource? _videoSource;

    [ObservableProperty]
    private ImageSource? _histogramSource;

    [ObservableProperty]
    private string? _currentVideoFile;

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private bool _hasVideo;

    [ObservableProperty]
    private HistogramOverlay _overlay = HistogramOverlay.Overlay;

    public MainWindowViewModel()
    {
        _playbackTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) }; // ~30 FPS
        _playbackTimer.Tick += OnPlaybackTimerTick;

        _histogramTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) }; // 1 second interval
        _histogramTimer.Tick += OnHistogramTimerTick;
    }

    [RelayCommand]
    private void OpenVideoFile()
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "Select Video File",
            Filter = "Video Files|*.mp4;*.avi;*.mov;*.mkv;*.wmv;*.flv;*.webm|All Files|*.*"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            LoadVideoFile(openFileDialog.FileName);
        }
    }

    [RelayCommand]
    private void SetOverlay(HistogramOverlay overlay)
    {
        Overlay = overlay;
    }

    private void LoadVideoFile(string filePath)
    {
        try
        {
            StopVideo();

            if (!File.Exists(filePath))
                return;

            _videoCapture?.Release();
            _videoCapture = new VideoCapture(filePath);

            if (!_videoCapture.IsOpened())
                return;

            CurrentVideoFile = Path.GetFileName(filePath);
            HasVideo = true;

            // Load first frame
            _currentFrame = new Mat();
            if (_videoCapture.Read(_currentFrame) && !_currentFrame.Empty())
            {
                UpdateVideoFrame();
                UpdateHistogram();
            }
        }
        catch (Exception ex)
        {
            // Log error or show message to user
            System.Diagnostics.Debug.WriteLine($"Error loading video: {ex.Message}");
        }
    }

    [RelayCommand(CanExecute = nameof(CanPlayPause))]
    private void PlayPause()
    {
        if (IsPlaying)
        {
            StopVideo();
        }
        else
        {
            StartVideo();
        }
    }

    [RelayCommand]
    private void Skip(string seconds)
    {
        if (_videoCapture?.IsOpened() == true &&
            int.TryParse(seconds, out int intSeconds))
        {
            var fps = _videoCapture.Fps;
            var currentFramePos = _videoCapture.Get(VideoCaptureProperties.PosFrames);
            var newFramePos = currentFramePos + (intSeconds * fps);
            newFramePos = Math.Max(0, Math.Min(newFramePos, _videoCapture.FrameCount - 1));
            _videoCapture.Set(VideoCaptureProperties.PosFrames, newFramePos);
            // Read the new frame
            _currentFrame = new Mat();
            if (_videoCapture.Read(_currentFrame) && !_currentFrame.Empty())
            {
                UpdateVideoFrame();
                UpdateHistogram();
            }
        }
    }

    private bool CanPlayPause() => HasVideo;

    private void StartVideo()
    {
        if (_videoCapture?.IsOpened() == true)
        {
            IsPlaying = true;
            _playbackTimer?.Start();
            _histogramTimer?.Start();
        }
    }

    private void StopVideo()
    {
        IsPlaying = false;
        _playbackTimer?.Stop();
        _histogramTimer?.Stop();
    }

    private void OnPlaybackTimerTick(object? sender, EventArgs e)
    {
        if (_videoCapture?.IsOpened() == true && _currentFrame != null)
        {
            if (_videoCapture.Read(_currentFrame) && !_currentFrame.Empty())
            {
                UpdateVideoFrame();
            }
            else
            {
                // End of video, restart from beginning
                _videoCapture.Set(VideoCaptureProperties.PosFrames, 0);
            }
        }
    }

    private void OnHistogramTimerTick(object? sender, EventArgs e)
    {
        UpdateHistogram();
    }

    private void UpdateVideoFrame()
    {
        if (_currentFrame?.Empty() == false)
        {
            VideoSource = MatToBitmapSource(_currentFrame);
        }
    }

    private void UpdateHistogram()
    {
        if (_currentFrame?.Empty() == false)
        {
            var histogram = CalculateHistogram(_currentFrame);
            HistogramSource = MatToBitmapSource(histogram);
        }
    }

    private static Mat CalculateHistogram(Mat frame)
    {
        try
        {
            // Convert BGR to RGB for proper color representation
            var rgbFrame = new Mat();
            Cv2.CvtColor(frame, rgbFrame, ColorConversionCodes.BGR2RGB);

            // Split channels
            var channels = Cv2.Split(rgbFrame);

            // Calculate histogram for each channel
            const int histSize = 256;
            const int histWidth = 512;
            const int histHeight = 400;
            const int binWidth = histWidth / histSize;

            var histImage = new Mat(histHeight, histWidth, MatType.CV_8UC3, Scalar.All(0));

            var histRange = new Rangef(0, 256);
            var ranges = new[] { histRange };

            var redHist = new Mat();
            var greenHist = new Mat();
            var blueHist = new Mat();

            Cv2.CalcHist([channels[0]], [0], null, redHist, 1, [histSize], ranges);
            Cv2.CalcHist([channels[1]], [0], null, greenHist, 1, [histSize], ranges);
            Cv2.CalcHist([channels[2]], [0], null, blueHist, 1, [histSize], ranges);

            // Normalize histograms
            Cv2.Normalize(redHist, redHist, 0, histImage.Rows, NormTypes.MinMax, -1);
            Cv2.Normalize(greenHist, greenHist, 0, histImage.Rows, NormTypes.MinMax, -1);
            Cv2.Normalize(blueHist, blueHist, 0, histImage.Rows, NormTypes.MinMax, -1);

            // Draw histogram
            for (int i = 1; i < histSize; i++)
            {
                var redVal = (int)redHist.At<float>(i);
                var greenVal = (int)greenHist.At<float>(i);
                var blueVal = (int)blueHist.At<float>(i);

                Cv2.Line(histImage,
                    new Point(binWidth * (i - 1), histHeight - (int)redHist.At<float>(i - 1)),
                    new Point(binWidth * i, histHeight - redVal),
                    Scalar.Red, 2);

                Cv2.Line(histImage,
                    new Point(binWidth * (i - 1), histHeight - (int)greenHist.At<float>(i - 1)),
                    new Point(binWidth * i, histHeight - greenVal),
                    Scalar.Green, 2);

                Cv2.Line(histImage,
                    new Point(binWidth * (i - 1), histHeight - (int)blueHist.At<float>(i - 1)),
                    new Point(binWidth * i, histHeight - blueVal),
                    Scalar.Blue, 2);
            }

            // Clean up
            rgbFrame.Dispose();
            channels[0].Dispose();
            channels[1].Dispose();
            channels[2].Dispose();
            redHist.Dispose();
            greenHist.Dispose();
            blueHist.Dispose();

            return histImage;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error calculating histogram: {ex.Message}");
            // Return black image on error
            return new Mat(400, 512, MatType.CV_8UC3, Scalar.All(0));
        }
    }

    private static BitmapSource MatToBitmapSource(Mat mat) => mat.ToBitmapSource();

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        
        if (e.PropertyName == nameof(HasVideo))
        {
            PlayPauseCommand.NotifyCanExecuteChanged();
        }
    }

    public void Dispose()
    {
        StopVideo();
        _playbackTimer?.Stop();
        _histogramTimer?.Stop();
        _videoCapture?.Release();
        _currentFrame?.Dispose();
    }
}
