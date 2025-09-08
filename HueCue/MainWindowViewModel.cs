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

    public MainWindowViewModel()
    {
        _playbackTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) }; // ~30 FPS
        _playbackTimer.Tick += OnPlaybackTimerTick;

        _histogramTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) }; // 1 second interval
        _histogramTimer.Tick += OnHistogramTimerTick;

        InitializeFaceDetection();
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
            var frameWithFaces = DetectFaces(_currentFrame);
            VideoSource = MatToBitmapSource(frameWithFaces);
            frameWithFaces.Dispose();
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

    private void InitializeFaceDetection()
    {
        try
        {
            // Initialize face detection - we'll use OpenCV's built-in capabilities
            // This serves as a placeholder for DNN model loading if models become available
            System.Diagnostics.Debug.WriteLine("Face detection initialized with color-based detection");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Face detection initialization failed: {ex.Message}");
        }
    }

    private Mat DetectFaces(Mat frame)
    {
        try
        {
            if (frame.Empty())
                return frame.Clone();

            // Create a copy of the frame to draw on
            var result = frame.Clone();

            // Implement face detection using skin color detection as a base approach
            // This can be extended with DNN models when available
            using var hsv = new Mat();
            Cv2.CvtColor(frame, hsv, ColorConversionCodes.BGR2HSV);

            // Enhanced skin color detection with multiple ranges
            var lowerSkin1 = new Scalar(0, 40, 60);    // Lower hue range
            var upperSkin1 = new Scalar(25, 255, 255);
            
            var lowerSkin2 = new Scalar(160, 40, 60);  // Upper hue range (wrapping around)
            var upperSkin2 = new Scalar(180, 255, 255);

            // Create masks for both skin color ranges
            using var skinMask1 = new Mat();
            using var skinMask2 = new Mat();
            using var skinMask = new Mat();
            
            Cv2.InRange(hsv, lowerSkin1, upperSkin1, skinMask1);
            Cv2.InRange(hsv, lowerSkin2, upperSkin2, skinMask2);
            Cv2.BitwiseOr(skinMask1, skinMask2, skinMask);

            // Apply morphological operations to clean up the mask
            using var kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(7, 7));
            using var cleanMask = new Mat();
            Cv2.MorphologyEx(skinMask, cleanMask, MorphTypes.Open, kernel);
            Cv2.MorphologyEx(cleanMask, cleanMask, MorphTypes.Close, kernel);

            // Apply additional blur to smooth the mask
            using var blurred = new Mat();
            Cv2.GaussianBlur(cleanMask, blurred, new Size(5, 5), 0);

            // Find contours in the cleaned mask
            Cv2.FindContours(blurred, out var contours, out var hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            // Analyze contours and draw yellow rectangles around potential faces
            foreach (var contour in contours)
            {
                var area = Cv2.ContourArea(contour);
                
                // Filter by area - faces should be reasonably sized
                if (area > 1200 && area < frame.Width * frame.Height * 0.3)
                {
                    var boundingRect = Cv2.BoundingRect(contour);
                    
                    // Check for face-like proportions and size constraints
                    var aspectRatio = (double)boundingRect.Width / boundingRect.Height;
                    var widthRatio = (double)boundingRect.Width / frame.Width;
                    var heightRatio = (double)boundingRect.Height / frame.Height;
                    
                    // Face should have reasonable proportions and size
                    if (aspectRatio > 0.6 && aspectRatio < 1.6 && 
                        boundingRect.Width > 40 && boundingRect.Height > 40 &&
                        widthRatio < 0.8 && heightRatio < 0.8)
                    {
                        // Additional check: look for facial features within the region
                        if (HasFacialFeatures(frame, boundingRect))
                        {
                            // Draw yellow rectangle around detected face
                            Cv2.Rectangle(result, boundingRect, Scalar.Yellow, 3);
                        }
                    }
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Face detection error: {ex.Message}");
            return frame.Clone();
        }
    }

    private static bool HasFacialFeatures(Mat frame, Rect faceRegion)
    {
        try
        {
            // Extract the face region
            using var faceRoi = new Mat(frame, faceRegion);
            using var grayFace = new Mat();
            Cv2.CvtColor(faceRoi, grayFace, ColorConversionCodes.BGR2GRAY);

            // Apply edge detection to look for facial features
            using var edges = new Mat();
            Cv2.Canny(grayFace, edges, 50, 150);

            // Count edge pixels - faces should have sufficient detail
            var edgePixels = Cv2.CountNonZero(edges);
            var totalPixels = faceRoi.Width * faceRoi.Height;
            var edgeRatio = (double)edgePixels / totalPixels;

            // Faces typically have an edge ratio between 0.1 and 0.4
            return edgeRatio > 0.1 && edgeRatio < 0.4;
        }
        catch
        {
            return true; // If feature detection fails, assume it's a face
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
}
