using System.ComponentModel;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;

using Microsoft.Win32;

using Velopack;

namespace HueCue;

public partial class MainWindowViewModel : ObservableObject
{
    private VideoCapture? _videoCapture;
    private DispatcherTimer? _playbackTimer;
    private DispatcherTimer? _histogramTimer;
    private DispatcherTimer? _liveStreamTimer;
    private Mat? _currentFrame;
    private FaceDetectorYN? _faceDetector;
    private AjaHeloStreamSource? _streamSource;

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
    private bool _faceDetectionEnabled = true;

    [ObservableProperty]
    private bool _faceDetectionAvailable = true;

    [ObservableProperty]
    private int _detectedFaceCount;

    [ObservableProperty]
    private HistogramOverlay _overlay = HistogramOverlay.Below;

    [ObservableProperty]
    private bool _isLiveStreaming;

    [ObservableProperty]
    private GuideOverlay _guideOverlay = GuideOverlay.None;

    public MainWindowViewModel()
    {
        _playbackTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) }; // ~30 FPS
        _playbackTimer.Tick += OnPlaybackTimerTick;

        _histogramTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) }; // 1 second interval
        _histogramTimer.Tick += OnHistogramTimerTick;

        _liveStreamTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) }; // 4 FPS (250ms)
        _liveStreamTimer.Tick += OnLiveStreamTimerTick;

        InitializeFaceDetector();
    }

    private void InitializeFaceDetector()
    {
        try
        {
            // Try to create face detector with default parameters
            // FaceDetectorYN requires a model file - let's try with an empty string first
            // which should use the built-in model if available
            _faceDetector = new FaceDetectorYN(
                model: "Resources/face_detection_yunet_2023mar.onnx", // Empty string should use default model
                config: "",
                inputSize: new System.Drawing.Size(320, 240),
                scoreThreshold: 0.6f,
                nmsThreshold: 0.3f
            );
            
            FaceDetectionAvailable = true;
            System.Diagnostics.Debug.WriteLine("Face detector initialized successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to initialize face detector: {ex.Message}");
            System.Diagnostics.Debug.WriteLine("Face detection will be disabled. You may need to provide a YuNet model file.");
            _faceDetector = null;
            FaceDetectionAvailable = false;
            // Disable face detection if initialization fails
            FaceDetectionEnabled = false;
        }
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
    private async Task LoadFromAjaHelo()
    {
        await LoadLiveStream();
    }

    [RelayCommand]
    private void SetOverlay(HistogramOverlay overlay)
    {
        Overlay = overlay;
    }

    [RelayCommand]
    private void SetGuideOverlay(GuideOverlay guideOverlay)
    {
        GuideOverlay = guideOverlay;
        
        // Update the current frame display
        if (_currentFrame?.IsEmpty == false && HasVideo)
        {
            UpdateVideoFrame();
        }
    }

    [RelayCommand]
    private void ToggleRuleOfThirdsGuide()
    {
        GuideOverlay = GuideOverlay == GuideOverlay.RuleOfThirds ? GuideOverlay.None : GuideOverlay.RuleOfThirds;
        
        // Update the current frame display
        if (_currentFrame?.IsEmpty == false && HasVideo)
        {
            UpdateVideoFrame();
        }
    }

    [RelayCommand]
    private void ToggleFaceDetection()
    {
        FaceDetectionEnabled = !FaceDetectionEnabled;
        
        // Update the current frame display
        if (_currentFrame?.IsEmpty == false && HasVideo)
        {
            UpdateVideoFrame();
        }
    }

    private void LoadVideoFile(string filePath)
    {
        try
        {
            StopVideo();
            StopLiveStream();

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

    private async Task LoadLiveStream()
    {
        try
        {
            StopVideo();
            StopLiveStream();

            _streamSource?.Dispose();
            _streamSource = new AjaHeloStreamSource();

            // Try to get initial frame to verify connection
            var initialFrame = await _streamSource.GetFrameAsync();
            if (initialFrame != null)
            {
                _currentFrame?.Dispose();
                _currentFrame = initialFrame;
                
                CurrentVideoFile = "AJA Helo Live Stream";
                HasVideo = true;
                IsLiveStreaming = true;

                UpdateVideoFrame();
                UpdateHistogram();

                // Start live stream timer
                _liveStreamTimer?.Start();
                _histogramTimer?.Start();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Failed to connect to AJA Helo stream");
                CurrentVideoFile = null;
                HasVideo = false;
                IsLiveStreaming = false;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading live stream: {ex.Message}");
            CurrentVideoFile = null;
            HasVideo = false;
            IsLiveStreaming = false;
        }
    }

    private void StopLiveStream()
    {
        _liveStreamTimer?.Stop();
        IsLiveStreaming = false;
        _streamSource?.Dispose();
        _streamSource = null;
    }

    [RelayCommand(CanExecute = nameof(CanPlayPause))]
    private void PlayPause()
    {
        if (IsLiveStreaming)
        {
            // For live streams, play/pause controls the display timer
            if (IsPlaying)
            {
                _liveStreamTimer?.Stop();
                _histogramTimer?.Stop();
                IsPlaying = false;
            }
            else
            {
                _liveStreamTimer?.Start();
                _histogramTimer?.Start();
                IsPlaying = true;
            }
        }
        else if (IsPlaying)
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
        _liveStreamTimer?.Stop();
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

    private async void OnLiveStreamTimerTick(object? sender, EventArgs e)
    {
        if (_streamSource != null && IsLiveStreaming)
        {
            try
            {
                var newFrame = await _streamSource.GetFrameAsync();
                if (newFrame != null)
                {
                    _currentFrame?.Dispose();
                    _currentFrame = newFrame;
                    UpdateVideoFrame();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting live stream frame: {ex.Message}");
            }
        }
    }

    private void UpdateVideoFrame()
    {
        if (_currentFrame?.IsEmpty == false)
        {
            Mat displayFrame = _currentFrame.Clone();
            
            if (FaceDetectionEnabled)
            {
                displayFrame = DetectAndDrawFaces(displayFrame);
            }

            if (GuideOverlay != GuideOverlay.None)
            {
                displayFrame = DrawGuideOverlays(displayFrame);
            }

            VideoSource = MatToBitmapSource(displayFrame);
            displayFrame.Dispose();
        }
    }

    private Mat DetectAndDrawFaces(Mat frame)
    {
        if (_faceDetector == null)
            return frame;

        try
        {
            // Resize frame for detection if needed (YuNet works better with specific input sizes)
            var detectionFrame = new Mat();
            var originalSize = frame.Size;
            var targetSize = new System.Drawing.Size(320, 240);
            
            CvInvoke.Resize(frame, detectionFrame, targetSize);
            
            // Update detector input size if needed
            if (_faceDetector.InputSize != targetSize)
            {
                _faceDetector.InputSize = targetSize;
            }

            // Detect faces
            var faces = new Mat();
            int detectionResult = _faceDetector.Detect(detectionFrame, faces);
            
            DetectedFaceCount = 0;

            if (detectionResult > 0 && !faces.IsEmpty)
            {
                // Get face detection results
                var faceData = new float[faces.Rows * faces.Cols];
                faces.CopyTo(faceData);
                
                var numFaces = faces.Rows;
                DetectedFaceCount = numFaces;

                // Scale factors to convert from detection frame to original frame
                double scaleX = (double)originalSize.Width / targetSize.Width;
                double scaleY = (double)originalSize.Height / targetSize.Height;

                // Draw rectangles around detected faces
                for (int i = 0; i < numFaces; i++)
                {
                    // Each face has 15 values: [x, y, w, h, x_re, y_re, x_le, y_le, x_nt, y_nt, x_rcm, y_rcm, x_lcm, y_lcm, score]
                    // We only need the first 4 for the bounding box
                    int baseIndex = i * 15;
                    
                    if (baseIndex + 3 < faceData.Length)
                    {
                        float x = faceData[baseIndex] * (float)scaleX;
                        float y = faceData[baseIndex + 1] * (float)scaleY;
                        float w = faceData[baseIndex + 2] * (float)scaleX;
                        float h = faceData[baseIndex + 3] * (float)scaleY;
                        float score = faceData[baseIndex + 14];

                        // Only draw if confidence is high enough
                        if (score > 0.5f)
                        {
                            var rect = new System.Drawing.Rectangle(
                                (int)x, (int)y, (int)w, (int)h);

                            // Draw bounding box
                            CvInvoke.Rectangle(frame, rect, new MCvScalar(0, 255, 0), 2);
                            
                            // Draw confidence score
                            string scoreText = $"{score:F2}";
                            var textPoint = new System.Drawing.Point((int)x, (int)y - 10);
                            CvInvoke.PutText(frame, scoreText, textPoint, 
                                FontFace.HersheySimplex, 0.6, new MCvScalar(0, 255, 0), 2);

                            // Draw facial landmarks if available
                            DrawFacialLandmarks(frame, faceData, baseIndex, scaleX, scaleY);
                        }
                    }
                }
            }

            detectionFrame.Dispose();
            faces.Dispose();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Face detection error: {ex.Message}");
        }

        return frame;
    }

    private void DrawFacialLandmarks(Mat frame, float[] faceData, int baseIndex, double scaleX, double scaleY)
    {
        try
        {
            // Draw facial landmarks (eyes, nose, mouth corners)
            var landmarks = new[]
            {
                // Right eye, Left eye, Nose tip, Right mouth corner, Left mouth corner
                new System.Drawing.Point((int)(faceData[baseIndex + 4] * scaleX), (int)(faceData[baseIndex + 5] * scaleY)),  // Right eye
                new System.Drawing.Point((int)(faceData[baseIndex + 6] * scaleX), (int)(faceData[baseIndex + 7] * scaleY)),  // Left eye
                new System.Drawing.Point((int)(faceData[baseIndex + 8] * scaleX), (int)(faceData[baseIndex + 9] * scaleY)),  // Nose tip
                new System.Drawing.Point((int)(faceData[baseIndex + 10] * scaleX), (int)(faceData[baseIndex + 11] * scaleY)), // Right mouth corner
                new System.Drawing.Point((int)(faceData[baseIndex + 12] * scaleX), (int)(faceData[baseIndex + 13] * scaleY))  // Left mouth corner
            };

            // Draw landmark points
            foreach (var landmark in landmarks)
            {
                CvInvoke.Circle(frame, landmark, 2, new MCvScalar(255, 0, 0), -1);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error drawing facial landmarks: {ex.Message}");
        }
    }

    private Mat DrawGuideOverlays(Mat frame)
    {
        switch (GuideOverlay)
        {
            case GuideOverlay.RuleOfThirds:
                return DrawRuleOfThirdsGuides(frame);
            default:
                return frame;
        }
    }

    private Mat DrawRuleOfThirdsGuides(Mat frame)
    {
        try
        {
            int width = frame.Width;
            int height = frame.Height;
            
            // Red color for guide lines (BGR format in OpenCV)
            var lineColor = new MCvScalar(0, 0, 255); // Red color
            int lineThickness = 2;
            
            // Calculate positions for rule of thirds lines
            int verticalLine1 = width / 3;
            int verticalLine2 = 2 * width / 3;
            int horizontalLine1 = height / 3;
            int horizontalLine2 = 2 * height / 3;
            
            // Draw vertical lines
            CvInvoke.Line(frame, 
                new System.Drawing.Point(verticalLine1, 0), 
                new System.Drawing.Point(verticalLine1, height), 
                lineColor, lineThickness);
            
            CvInvoke.Line(frame, 
                new System.Drawing.Point(verticalLine2, 0), 
                new System.Drawing.Point(verticalLine2, height), 
                lineColor, lineThickness);
            
            // Draw horizontal lines
            CvInvoke.Line(frame, 
                new System.Drawing.Point(0, horizontalLine1), 
                new System.Drawing.Point(width, horizontalLine1), 
                lineColor, lineThickness);
            
            CvInvoke.Line(frame, 
                new System.Drawing.Point(0, horizontalLine2), 
                new System.Drawing.Point(width, horizontalLine2), 
                lineColor, lineThickness);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error drawing rule of thirds guides: {ex.Message}");
        }
        
        return frame;
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
        return mat.ToBitmapSource();
    }

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
        StopLiveStream();
        _playbackTimer?.Stop();
        _histogramTimer?.Stop();
        _liveStreamTimer?.Stop();
        _videoCapture?.Dispose();
        _currentFrame?.Dispose();
        _faceDetector?.Dispose();
        _streamSource?.Dispose();
    }
}
