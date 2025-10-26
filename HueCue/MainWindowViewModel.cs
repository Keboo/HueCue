using System.ComponentModel;
using System.IO;
using System.Threading.Channels;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;

using HueCue.Sources;

using Microsoft.Win32;

namespace HueCue;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly DispatcherTimer _playbackTimer = new();
    private readonly IDispatcher _dispatcher;
    private readonly Channel<Mat> _frameChannel;
    private Mat? _currentFrame;
    private FaceDetectorYN? _faceDetector;
    private IStreamSource? _currentStreamSource;

    // Define 12 visually distinct colors for face bounding boxes (BGR format for OpenCV)
    private static readonly MCvScalar[] FaceColors =
    [
        new MCvScalar(0, 255, 0),      // Green
        new MCvScalar(255, 0, 0),      // Blue
        new MCvScalar(0, 0, 255),      // Red
        new MCvScalar(0, 255, 255),    // Yellow
        new MCvScalar(255, 0, 255),    // Magenta
        new MCvScalar(255, 255, 0),    // Cyan
        new MCvScalar(0, 165, 255),    // Orange
        new MCvScalar(128, 0, 128),    // Purple
        new MCvScalar(255, 192, 203),  // Pink
        new MCvScalar(0, 255, 127),    // Spring Green
        new MCvScalar(255, 255, 255),  // White
        new MCvScalar(64, 224, 208)    // Turquoise
    ];

    [ObservableProperty]
    private ImageSource? _videoSource;

    [ObservableProperty]
    private ImageSource? _histogramSource;

    [ObservableProperty]
    private string? _currentVideoFile;

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PlayPauseCommand))]
    private bool _hasVideo;

    [ObservableProperty]
    private bool _faceDetectionEnabled = true;

    partial void OnFaceDetectionEnabledChanged(bool oldValue, bool newValue)
    {
        // Update the current frame display
        if (_currentFrame?.IsEmpty == false && HasVideo)
        {
            UpdateVideoFrame();
        }
    }

    [ObservableProperty]
    private bool _faceDetectionAvailable = true;

    [ObservableProperty]
    private int _detectedFaceCount;

    [ObservableProperty]
    private HistogramOverlay _overlay = HistogramOverlay.Right;

    [ObservableProperty]
    private GuideOverlay _guideOverlay = GuideOverlay.None;

    [ObservableProperty]
    private bool _topMost;

    [ObservableProperty]
    private double _windowOpacity = 1.0;

    public MainWindowViewModel(IDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        _dispatcher = dispatcher;
        _frameChannel = Channel.CreateBounded<Mat>(new BoundedChannelOptions(1)
        {
            SingleReader = true,
            FullMode = BoundedChannelFullMode.DropOldest
        });

        _ = Task.Run(async () => await ProcessFrameImage(_frameChannel.Reader));

        _playbackTimer.Tick += OnPlaybackTimerTick;

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
    private async Task OpenVideoFile()
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "Select Video or Image File",
            Filter = "Video and Image Files|*.mp4;*.avi;*.mov;*.mkv;*.wmv;*.flv;*.webm;*.jpg;*.jpeg;*.png;*.bmp;*.tiff;*.tif;*.gif|Video Files|*.mp4;*.avi;*.mov;*.mkv;*.wmv;*.flv;*.webm|Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.tiff;*.tif;*.gif|All Files|*.*"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            await LoadFileAsync(openFileDialog.FileName);
        }
    }

    [RelayCommand]
    private async Task LoadFromAjaHelo()
    {
        StopVideo();

        Interlocked.Exchange(ref _currentStreamSource, new AjaHeloStreamSource())?.Dispose();

        await StartVideoAsync();
    }

    [RelayCommand]
    private async Task LoadFromSubsplash()
    {
        StopVideo();
        Interlocked.Exchange(ref _currentStreamSource, new SubsplashStreamSource())?.Dispose();
        await StartVideoAsync();
    }

    [RelayCommand]
    private void SetOverlay(HistogramOverlay overlay)
    {
        Overlay = overlay;
    }

    [RelayCommand]
    private void SetGuideOverlay(GuideOverlay guideOverlay)
    {
        // Toggle off if the same overlay is already active, otherwise set the new overlay
        GuideOverlay = GuideOverlay == guideOverlay ? GuideOverlay.None : guideOverlay;

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
    private void SetWindowOpacity(string opacity)
    {
        if (double.TryParse(opacity, out double opacityValue))
        {
            opacityValue = Math.Max(0.1, Math.Min(1.0, opacityValue));
            WindowOpacity = opacityValue;
        }
    }

    private async Task LoadFileAsync(string filePath)
    {
        StopVideo();
        if (!File.Exists(filePath))
            return;
        if (IsImageFile(filePath))
        {
            Interlocked.Exchange(ref _currentStreamSource, new ImageStreamSource(filePath))?.Dispose();
        }
        else
        {
            Interlocked.Exchange(ref _currentStreamSource, new VideoFileStreamSource(filePath))?.Dispose();
        }
        await StartVideoAsync();

        static bool IsImageFile(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension is ".jpg" or ".jpeg" or ".png" or ".bmp" or ".tiff" or ".tif" or ".gif";
        }
    }

    [RelayCommand(CanExecute = nameof(CanPlayPause))]
    private async Task PlayPause()
    {
        if (IsPlaying)
        {
            StopVideo();
        }
        else
        {
            await StartVideoAsync();
        }
    }

    [RelayCommand]
    private void Skip(string seconds)
    {
        //TODO: Forward to source?

        //if (_videoCapture?.IsOpened == true &&
        //    int.TryParse(seconds, out int intSeconds))
        //{
        //    var fps = _videoCapture.Get(CapProp.Fps);
        //    var currentFramePos = _videoCapture.Get(CapProp.PosFrames);
        //    var newFramePos = currentFramePos + (intSeconds * fps);
        //    newFramePos = Math.Max(0, Math.Min(newFramePos, _videoCapture.Get(CapProp.FrameCount) - 1));
        //    _videoCapture.Set(CapProp.PosFrames, newFramePos);
        //    // Read the new frame
        //    _currentFrame = new Mat();
        //    if (_videoCapture.Read(_currentFrame) && !_currentFrame.IsEmpty)
        //    {
        //        UpdateVideoFrame();
        //        UpdateHistogram();
        //    }
        //}
    }

    private bool CanPlayPause() => HasVideo;

    private async ValueTask StartVideoAsync()
    {
        if (_currentStreamSource is { } streamSource)
        {
            if (!await streamSource.LoadAsync())
            {
                return;
            }

            HasVideo = true;
            _playbackTimer.Stop();

            if (streamSource.Fps > 0)
            {
                _playbackTimer.Interval = TimeSpan.FromSeconds(1 / streamSource.Fps);
                _playbackTimer.Start();
            }
            else
            {
                //Skip the time if the source does not have an FPS
                await ShowFrameImage(streamSource);
            }

            IsPlaying = true;
        }
        else
        {
            HasVideo = false;
            IsPlaying = false;
        }

        //if (_videoCapture?.IsOpened == true)
        //{
        //    IsPlaying = true;
        //    _playbackTimer?.Start();
        //    _histogramTimer?.Start();
        //}
    }

    private void StopVideo()
    {
        IsPlaying = false;
        _playbackTimer.Stop();
        //_histogramTimer?.Stop();
        //_liveStreamTimer?.Stop();
    }

    private async void OnPlaybackTimerTick(object? sender, EventArgs e)
    {
        if (_currentStreamSource is { } streamSource)
        {
            await ShowFrameImage(streamSource);

            //if (_videoCapture?.IsOpened == true && _currentFrame != null)
            //{
            //    if (_videoCapture.Read(_currentFrame) && !_currentFrame.IsEmpty)
            //    {
            //        UpdateVideoFrame();
            //    }
            //    else
            //    {
            //        // End of video, restart from beginning
            //        _videoCapture.Set(CapProp.PosFrames, 0);
            //    }
            //}
        }
    }

    private async Task ShowFrameImage(IStreamSource streamSource)
    {
        //TODO: this introduces latency
        using Mat? frame = await streamSource.GetFrameAsync();
        if (frame?.IsEmpty == false)
        {
            //TODO: Needed?
            Interlocked.Exchange(ref _currentFrame, frame.Clone())?.Dispose();
            //NB: Used for processing, the clone is disposed by the reader
            _frameChannel.Writer.TryWrite(frame.Clone());
            VideoSource = MatToBitmapSource(frame);
        }
        else
        {
            StopVideo();
        }
    }

    private async ValueTask ProcessFrameImage(ChannelReader<Mat> reader)
    {
        while (await reader.WaitToReadAsync())
        {
            while (reader.TryRead(out Mat? image))
            {
                try
                {
                    if (Overlay != HistogramOverlay.None)
                    {
                        var histogram = CalculateHistogram(image);
                        HistogramSource = MatToBitmapSource(histogram);
                    }
                    if (FaceDetectionEnabled)
                    {
                        using var processedFrame = DetectAndDrawFaces(image);
                        VideoSource = MatToBitmapSource(processedFrame);
                    }
                }
                finally
                {
                    image.Dispose();
                }
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
            Mat displayFrame = _currentFrame.Clone();

            if (FaceDetectionEnabled)
            {
                displayFrame = DetectAndDrawFaces(displayFrame);
            }

            // Guide overlays are now rendered as WPF elements, not drawn on the frame

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
                        if (score > 0.4f)
                        {
                            var rect = new System.Drawing.Rectangle(
                                (int)x, (int)y, (int)w, (int)h);

                            // Get color for this face (cycle through colors if more than 12 faces)
                            var color = FaceColors[i % FaceColors.Length];

                            // Draw bounding box
                            CvInvoke.Rectangle(frame, rect, color, 2);

                            // Draw confidence score
                            string scoreText = $"{score:F2}";
                            var textPoint = new System.Drawing.Point((int)x, (int)y - 10);
                            CvInvoke.PutText(frame, scoreText, textPoint,
                                FontFace.HersheySimplex, 0.6, color, 2);

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

    private static void DrawFacialLandmarks(Mat frame, float[] faceData, int baseIndex, double scaleX, double scaleY)
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
            CvInvoke.CalcHist(new VectorOfMat([channels[0]]), [0], null, redHist, [histSize], ranges, false);
            CvInvoke.CalcHist(new VectorOfMat([channels[1]]), [0], null, greenHist, [histSize], ranges, false);
            CvInvoke.CalcHist(new VectorOfMat([channels[2]]), [0], null, blueHist, [histSize], ranges, false);

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

    private BitmapSource MatToBitmapSource(Mat mat)
    {
        return _dispatcher.Invoke(mat.ToBitmapSource);
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
        _playbackTimer.Stop();
        _currentFrame?.Dispose();
        _faceDetector?.Dispose();
    }
}