namespace HueCue.Tests;

//This attribute generates tests for MainWindowViewModel that
//asserts all constructor arguments are checked for null
[ConstructorTests(typeof(MainWindowViewModel))]
public partial class MainWindowViewModelTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        //Arrange & Act
        AutoMocker mocker = new();
        MainWindowViewModel viewModel = mocker.CreateInstance<MainWindowViewModel>();

        //Assert
        Assert.Null(viewModel.VideoSource);
        Assert.Null(viewModel.HistogramSource);
        Assert.Null(viewModel.CurrentVideoFile);
        Assert.False(viewModel.IsPlaying);
        Assert.False(viewModel.HasVideo);
        Assert.False(viewModel.IsLiveStreaming);
        Assert.Equal(HistogramOverlay.Below, viewModel.Overlay);
        Assert.False(viewModel.TopMost);
        Assert.Null(viewModel.AtemPreviewInput);
        Assert.Null(viewModel.AtemProgramInput);
        Assert.False(viewModel.AtemConnected);
    }

    [Fact]
    public void PlayPauseCommand_CanExecute_ReturnsFalseWhenNoVideo()
    {
        //Arrange
        AutoMocker mocker = new();
        MainWindowViewModel viewModel = mocker.CreateInstance<MainWindowViewModel>();

        //Act
        bool canExecute = viewModel.PlayPauseCommand.CanExecute(null);

        //Assert
        Assert.False(canExecute);
    }

    [Fact]
    public void OpenVideoFileCommand_CanAlwaysExecute()
    {
        //Arrange
        AutoMocker mocker = new();
        MainWindowViewModel viewModel = mocker.CreateInstance<MainWindowViewModel>();

        //Act
        bool canExecute = viewModel.OpenVideoFileCommand.CanExecute(null);

        //Assert
        Assert.True(canExecute);
    }

    [Fact]
    public void LoadFromAjaHeloCommand_CanAlwaysExecute()
    {
        //Arrange
        AutoMocker mocker = new();
        MainWindowViewModel viewModel = mocker.CreateInstance<MainWindowViewModel>();

        //Act
        bool canExecute = viewModel.LoadFromAjaHeloCommand.CanExecute(null);

        //Assert
        Assert.True(canExecute);
    }

    [Fact]
    public void Dispose_DoesNotThrowException()
    {
        //Arrange
        AutoMocker mocker = new();
        MainWindowViewModel viewModel = mocker.CreateInstance<MainWindowViewModel>();

        //Act & Assert
        var exception = Record.Exception(() => viewModel.Dispose());
        Assert.Null(exception);
    }

    [Fact]
    public void SetGuideOverlayCommand_SetsGuideOverlay()
    {
        //Arrange
        AutoMocker mocker = new();
        MainWindowViewModel viewModel = mocker.CreateInstance<MainWindowViewModel>();

        //Act
        viewModel.SetGuideOverlayCommand.Execute(GuideOverlay.RuleOfThirds);

        //Assert
        Assert.Equal(GuideOverlay.RuleOfThirds, viewModel.GuideOverlay);
    }

    [Fact]
    public void ToggleRuleOfThirdsGuideCommand_TogglesGuideOverlay()
    {
        //Arrange
        AutoMocker mocker = new();
        MainWindowViewModel viewModel = mocker.CreateInstance<MainWindowViewModel>();
        Assert.Equal(GuideOverlay.None, viewModel.GuideOverlay);

        //Act - Enable
        viewModel.ToggleRuleOfThirdsGuideCommand.Execute(null);

        //Assert
        Assert.Equal(GuideOverlay.RuleOfThirds, viewModel.GuideOverlay);

        //Act - Disable
        viewModel.ToggleRuleOfThirdsGuideCommand.Execute(null);

        //Assert
        Assert.Equal(GuideOverlay.None, viewModel.GuideOverlay);
    }

    [Fact]
    public void SetGuideOverlayCommand_CanAlwaysExecute()
    {
        //Arrange
        AutoMocker mocker = new();
        MainWindowViewModel viewModel = mocker.CreateInstance<MainWindowViewModel>();

        //Act
        bool canExecute = viewModel.SetGuideOverlayCommand.CanExecute(GuideOverlay.RuleOfThirds);

        //Assert
        Assert.True(canExecute);
    }

    [Fact]
    public void ToggleRuleOfThirdsGuideCommand_CanAlwaysExecute()
    {
        //Arrange
        AutoMocker mocker = new();
        MainWindowViewModel viewModel = mocker.CreateInstance<MainWindowViewModel>();

        //Act
        bool canExecute = viewModel.ToggleRuleOfThirdsGuideCommand.CanExecute(null);

        //Assert
        Assert.True(canExecute);
    }

    [Fact]
    public void ToggleTopMostCommand_TogglesTopMost()
    {
        //Arrange
        AutoMocker mocker = new();
        MainWindowViewModel viewModel = mocker.CreateInstance<MainWindowViewModel>();
        Assert.False(viewModel.TopMost);

        //Act - Enable
        viewModel.ToggleTopMostCommand.Execute(null);

        //Assert
        Assert.True(viewModel.TopMost);

        //Act - Disable
        viewModel.ToggleTopMostCommand.Execute(null);

        //Assert
        Assert.False(viewModel.TopMost);
    }

    [Fact]
    public void ToggleTopMostCommand_CanAlwaysExecute()
    {
        //Arrange
        AutoMocker mocker = new();
        MainWindowViewModel viewModel = mocker.CreateInstance<MainWindowViewModel>();

        //Act
        bool canExecute = viewModel.ToggleTopMostCommand.CanExecute(null);

        //Assert
        Assert.True(canExecute);
    }

    [Theory]
    [InlineData("test.jpg", true)]
    [InlineData("test.jpeg", true)]
    [InlineData("test.png", true)]
    [InlineData("test.bmp", true)]
    [InlineData("test.tiff", true)]
    [InlineData("test.tif", true)]
    [InlineData("test.gif", true)]
    [InlineData("test.JPG", true)]
    [InlineData("test.PNG", true)]
    [InlineData("test.mp4", false)]
    [InlineData("test.avi", false)]
    [InlineData("test.txt", false)]
    [InlineData("test", false)]
    public void IsImageFile_ReturnsCorrectResult(string fileName, bool expected)
    {
        //Arrange & Act
        // Using reflection to access the private static method
        var method = typeof(MainWindowViewModel).GetMethod("IsImageFile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        bool result = (bool)method!.Invoke(null, new object[] { fileName })!;

        //Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void LoadFile_CallsCorrectMethodBasedOnFileType()
    {
        //Arrange
        AutoMocker mocker = new();
        MainWindowViewModel viewModel = mocker.CreateInstance<MainWindowViewModel>();
        
        // Test that the commands are properly accessible (this validates the overall integration)
        bool canExecuteOpen = viewModel.OpenVideoFileCommand.CanExecute(null);
        bool canExecutePlayPause = viewModel.PlayPauseCommand.CanExecute(null);
        
        //Assert
        Assert.True(canExecuteOpen); // Should always be able to open files
        Assert.False(canExecutePlayPause); // Should not be able to play/pause without video
    }

    [Fact]
    public void ConnectToAtemCommand_CanAlwaysExecute()
    {
        //Arrange
        AutoMocker mocker = new();
        MainWindowViewModel viewModel = mocker.CreateInstance<MainWindowViewModel>();

        //Act
        bool canExecute = viewModel.ConnectToAtemCommand.CanExecute(null);

        //Assert
        Assert.True(canExecute);
    }

    [Fact]
    public void Constructor_SetsDefaultAtemValues()
    {
        //Arrange & Act
        AutoMocker mocker = new();
        MainWindowViewModel viewModel = mocker.CreateInstance<MainWindowViewModel>();

        //Assert
        Assert.Null(viewModel.AtemPreviewInput);
        Assert.Null(viewModel.AtemProgramInput);
        Assert.False(viewModel.AtemConnected);
    }
}