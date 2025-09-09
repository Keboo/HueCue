using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using System.Drawing;
using System.Drawing.Imaging;
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

            _videoCapture?.Dispose();
            _videoCapture = new VideoCapture(filePath);

            if (!_videoCapture.IsOpened)
                return;

            CurrentVideoFile = Path.GetFileName(filePath);
            HasVideo = true;

            // Load first frame
            _currentFrame = new Mat();
            if (_videoCapture.Read(_currentFrame) && !_currentFrame.IsEmpty)
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
        if (_videoCapture?.IsOpened == true &&
            int.TryParse(seconds, out int intSeconds))
        {
            var fps = _videoCapture.Get(CapProp.Fps);
            var currentFramePos = _videoCapture.Get(CapProp.PosFrames);
            var newFramePos = currentFramePos + (intSeconds * fps);
            newFramePos = Math.Max(0, Math.Min(newFramePos, _videoCapture.Get(CapProp.FrameCount) - 1));
            _videoCapture.Set(CapProp.PosFrames, newFramePos);
            // Read the new frame
            _currentFrame = new Mat();
            if (_videoCapture.Read(_currentFrame) && !_currentFrame.IsEmpty)
            {
                UpdateVideoFrame();
                UpdateHistogram();
            }
        }
    }

    private bool CanPlayPause() => HasVideo;

    private void StartVideo()
    {
        if (_videoCapture?.IsOpened == true)
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
        if (_videoCapture?.IsOpened == true && _currentFrame != null)
        {
            if (_videoCapture.Read(_currentFrame) && !_currentFrame.IsEmpty)
            {
                UpdateVideoFrame();
            }
            else
            {
                // End of video, restart from beginning
                _videoCapture.Set(CapProp.PosFrames, 0);
            }
        }
    }

    private void OnHistogramTimerTick(object? sender, EventArgs e)
    {
        UpdateHistogram();
    }

    private void UpdateVideoFrame()
    {
        if (_currentFrame?.IsEmpty == false)
        {
            VideoSource = MatToBitmapSource(_currentFrame);
        }
    }

    private void UpdateHistogram()
    {
        if (_currentFrame?.IsEmpty == false)
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
            CvInvoke.CvtColor(frame, rgbFrame, ColorConversion.Bgr2Rgb);

            // Split channels
            var channels = new VectorOfMat();
            CvInvoke.Split(rgbFrame, channels);

            // Calculate histogram for each channel
            const int histSize = 256;
            const int histWidth = 512;
            const int histHeight = 400;
            const int binWidth = histWidth / histSize;

            var histImage = new Mat(histHeight, histWidth, DepthType.Cv8U, 3);
            histImage.SetTo(new MCvScalar(0, 0, 0));

            var histRange = new float[] { 0, 256 };
            var ranges = histRange;

            var redHist = new Mat();
            var greenHist = new Mat();
            var blueHist = new Mat();

            // Calculate histograms
            CvInvoke.CalcHist(new VectorOfMat(new Mat[] { channels[0] }), new int[] { 0 }, null, redHist, new int[] { histSize }, ranges, false);
            CvInvoke.CalcHist(new VectorOfMat(new Mat[] { channels[1] }), new int[] { 0 }, null, greenHist, new int[] { histSize }, ranges, false);
            CvInvoke.CalcHist(new VectorOfMat(new Mat[] { channels[2] }), new int[] { 0 }, null, blueHist, new int[] { histSize }, ranges, false);

            // Normalize histograms
            CvInvoke.Normalize(redHist, redHist, 0, histImage.Rows, NormType.MinMax, DepthType.Cv32F);
            CvInvoke.Normalize(greenHist, greenHist, 0, histImage.Rows, NormType.MinMax, DepthType.Cv32F);
            CvInvoke.Normalize(blueHist, blueHist, 0, histImage.Rows, NormType.MinMax, DepthType.Cv32F);

            // Convert histograms to float arrays for easier access
            var redHistData = new float[histSize];
            var greenHistData = new float[histSize];
            var blueHistData = new float[histSize];

            redHist.CopyTo(redHistData);
            greenHist.CopyTo(greenHistData);
            blueHist.CopyTo(blueHistData);

            // Draw histogram
            for (int i = 1; i < histSize; i++)
            {
                var redVal = (int)redHistData[i];
                var greenVal = (int)greenHistData[i];
                var blueVal = (int)blueHistData[i];

                CvInvoke.Line(histImage,
                    new System.Drawing.Point(binWidth * (i - 1), histHeight - (int)redHistData[i - 1]),
                    new System.Drawing.Point(binWidth * i, histHeight - redVal),
                    new MCvScalar(0, 0, 255), 2); // Red channel

                CvInvoke.Line(histImage,
                    new System.Drawing.Point(binWidth * (i - 1), histHeight - (int)greenHistData[i - 1]),
                    new System.Drawing.Point(binWidth * i, histHeight - greenVal),
                    new MCvScalar(0, 255, 0), 2); // Green channel

                CvInvoke.Line(histImage,
                    new System.Drawing.Point(binWidth * (i - 1), histHeight - (int)blueHistData[i - 1]),
                    new System.Drawing.Point(binWidth * i, histHeight - blueVal),
                    new MCvScalar(255, 0, 0), 2); // Blue channel
            }

            // Clean up
            rgbFrame.Dispose();
            channels.Dispose();
            redHist.Dispose();
            greenHist.Dispose();
            blueHist.Dispose();

            return histImage;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error calculating histogram: {ex.Message}");
            // Return black image on error
            var errorMat = new Mat(400, 512, DepthType.Cv8U, 3);
            errorMat.SetTo(new MCvScalar(0, 0, 0));
            return errorMat;
        }
    }

    private static BitmapSource MatToBitmapSource(Mat mat)
    {
        try
        {
            // Convert Mat to System.Drawing.Bitmap
            var bitmap = mat.ToBitmap();
            
            // Determine the correct WPF pixel format based on the bitmap's pixel format
            PixelFormat pixelFormat = bitmap.PixelFormat switch
            {
                System.Drawing.Imaging.PixelFormat.Format24bppRgb => PixelFormats.Rgb24,
                System.Drawing.Imaging.PixelFormat.Format32bppRgb => PixelFormats.Rgb24,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb => PixelFormats.Bgra32,
                _ => PixelFormats.Bgr24
            };
            
            // Convert System.Drawing.Bitmap to BitmapSource
            var bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly,
                bitmap.PixelFormat);

            var bitmapSource = BitmapSource.Create(
                bitmapData.Width, bitmapData.Height,
                96, 96,
                pixelFormat,
                null,
                bitmapData.Scan0,
                bitmapData.Stride * bitmapData.Height,
                bitmapData.Stride);

            bitmap.UnlockBits(bitmapData);
            bitmap.Dispose();

            // Freeze the BitmapSource for performance
            bitmapSource.Freeze();
            return bitmapSource;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error converting Mat to BitmapSource: {ex.Message}");
            // Return a default black image
            return BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgr24, null, new byte[3], 3);
        }
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        
        if (e.PropertyName == nameof(HasVideo))
        {
            PlayPauseCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand]
    private async Task CheckForUpdates()
    {
        try
        {
            UpdateManager updateManager = new(new Velopack.Sources.VelopackFlowSource());
            if (updateManager.IsInstalled)
            {
                var updateInfo = await updateManager.CheckForUpdatesAsync();
                if (updateInfo != null)
                {
                    // Update available - you could show a dialog here
                    await updateManager.DownloadUpdatesAsync(updateInfo);
                    updateManager.ApplyUpdatesAndRestart(updateInfo);
                }
            }
        }
        catch (Exception ex)
        {
            // Handle update check errors
            System.Diagnostics.Debug.WriteLine($"Update check failed: {ex.Message}");
        }
    }

    public void Dispose()
    {
        StopVideo();
        _playbackTimer?.Stop();
        _histogramTimer?.Stop();
        _videoCapture?.Dispose();
        _currentFrame?.Dispose();
    }
}
