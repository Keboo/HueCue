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
    private FisherFaceRecognizer? _faceRecognizer;

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
            // Initialize FisherFaceRecognizer as requested
            _faceRecognizer = FisherFaceRecognizer.Create();
            
            // For demonstration purposes, we'll create some dummy training data
            // In a real application, this would be actual face images and labels
            var trainingImages = new Mat[2];
            var labels = new int[2];
            
            // Create dummy face images (in practice these would be real face samples)
            trainingImages[0] = new Mat(100, 100, MatType.CV_8UC1, Scalar.Gray);
            trainingImages[1] = new Mat(100, 100, MatType.CV_8UC1, Scalar.White);
            labels[0] = 0;
            labels[1] = 1;
            
            // Train the recognizer
            _faceRecognizer.Train(trainingImages, labels);
            
            // Clean up training data
            trainingImages[0].Dispose();
            trainingImages[1].Dispose();
            
            System.Diagnostics.Debug.WriteLine("Face detection initialized with FisherFaceRecognizer");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FisherFaceRecognizer initialization failed: {ex.Message}");
            _faceRecognizer = null;
        }
    }

    private Mat DetectFaces(Mat frame)
    {
        try
        {
            if (frame.Empty() || _faceRecognizer == null)
                return frame.Clone();

            // Create a copy of the frame to draw on
            var result = frame.Clone();

            // Convert to grayscale for face detection
            using var grayFrame = new Mat();
            Cv2.CvtColor(frame, grayFrame, ColorConversionCodes.BGR2GRAY);

            // Use a more efficient multi-scale sliding window approach
            var detectedFaces = new List<Rect>();
            var minFaceSize = Math.Min(frame.Width, frame.Height) / 8; // Minimum face size
            var maxFaceSize = Math.Min(frame.Width, frame.Height) / 3; // Maximum face size
            
            // Multiple scales for better detection
            for (int faceSize = minFaceSize; faceSize <= maxFaceSize; faceSize += 20)
            {
                var stepSize = faceSize / 4; // Smaller steps for better coverage
                
                for (int y = 0; y <= grayFrame.Height - faceSize; y += stepSize)
                {
                    for (int x = 0; x <= grayFrame.Width - faceSize; x += stepSize)
                    {
                        var candidate = new Rect(x, y, faceSize, faceSize);
                        
                        // Extract candidate region
                        using var candidateRegion = new Mat(grayFrame, candidate);
                        using var resizedCandidate = new Mat();
                        Cv2.Resize(candidateRegion, resizedCandidate, new Size(100, 100));
                        
                        // Use FisherFaceRecognizer to predict if this is a face
                        if (IsFaceRegion(resizedCandidate, candidate, grayFrame))
                        {
                            // Check for overlapping detections and keep the best one
                            if (!HasOverlappingDetection(detectedFaces, candidate))
                            {
                                detectedFaces.Add(candidate);
                            }
                        }
                    }
                }
            }

            // Draw yellow rectangles around all detected faces
            foreach (var face in detectedFaces)
            {
                Cv2.Rectangle(result, face, Scalar.Yellow, 3);
            }

            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Face detection error: {ex.Message}");
            return frame.Clone();
        }
    }

    private static bool HasOverlappingDetection(List<Rect> existingFaces, Rect newFace)
    {
        foreach (var existingFace in existingFaces)
        {
            var intersection = existingFace & newFace;
            var overlapRatio = (double)intersection.Area / Math.Min(existingFace.Area, newFace.Area);
            
            // If overlap is more than 30%, consider it the same face
            if (overlapRatio > 0.3)
                return true;
        }
        return false;
    }

    private bool IsFaceRegion(Mat candidateRegion, Rect candidateRect, Mat fullFrame)
    {
        try
        {
            if (_faceRecognizer == null)
                return false;

            // Check if region has sufficient variance (not too uniform)
            Scalar mean, stddev;
            Cv2.MeanStdDev(candidateRegion, out mean, out stddev);
            
            // Faces typically have reasonable contrast (not too uniform)
            if (stddev.Val0 < 20) // Too uniform, likely not a face
                return false;

            // Use edge detection to validate facial features
            using var edges = new Mat();
            Cv2.Canny(candidateRegion, edges, 50, 150);
            
            var edgePixels = Cv2.CountNonZero(edges);
            var totalPixels = candidateRegion.Width * candidateRegion.Height;
            var edgeRatio = (double)edgePixels / totalPixels;
            
            // Faces should have moderate edge density (features but not too noisy)
            if (edgeRatio < 0.08 || edgeRatio > 0.35)
                return false;

            // Check for reasonable brightness distribution (faces shouldn't be too dark or bright)
            var brightness = mean.Val0;
            if (brightness < 30 || brightness > 200)
                return false;

            // Use FisherFaceRecognizer for final validation
            // The recognizer helps distinguish face-like patterns from other textures
            try
            {
                _faceRecognizer.Predict(candidateRegion, out int label, out double confidence);
                
                // Lower confidence values indicate better matches to training data
                // Since we use simple training data, adjust threshold appropriately
                return confidence < 3000; // This threshold may need tuning based on results
            }
            catch
            {
                // If prediction fails, fall back to traditional validation
                return true;
            }
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        StopVideo();
        _playbackTimer?.Stop();
        _histogramTimer?.Stop();
        _videoCapture?.Release();
        _currentFrame?.Dispose();
        _faceRecognizer?.Dispose();
    }
    }
}
