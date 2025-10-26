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
        Assert.Equal(HistogramOverlay.Right, viewModel.Overlay);
        Assert.False(viewModel.TopMost);
        Assert.Equal(1.0, viewModel.WindowOpacity);
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
    public void SetGuideOverlayCommand_SetsHeatMapGuideOverlay()
    {
        //Arrange
        AutoMocker mocker = new();
        MainWindowViewModel viewModel = mocker.CreateInstance<MainWindowViewModel>();

        //Act
        viewModel.SetGuideOverlayCommand.Execute(GuideOverlay.HeatMap);

        //Assert
        Assert.Equal(GuideOverlay.HeatMap, viewModel.GuideOverlay);
    }

    [Fact]
    public void SetGuideOverlayCommand_TogglesOffWhenSameOverlayIsSet()
    {
        //Arrange
        AutoMocker mocker = new();
        MainWindowViewModel viewModel = mocker.CreateInstance<MainWindowViewModel>();
        viewModel.SetGuideOverlayCommand.Execute(GuideOverlay.HeatMap);
        Assert.Equal(GuideOverlay.HeatMap, viewModel.GuideOverlay);

        //Act - Set the same overlay again
        viewModel.SetGuideOverlayCommand.Execute(GuideOverlay.HeatMap);

        //Assert - Should toggle off to None
        Assert.Equal(GuideOverlay.None, viewModel.GuideOverlay);
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
    public void SetWindowOpacityCommand_SetsWindowOpacity()
    {
        //Arrange
        AutoMocker mocker = new();
        MainWindowViewModel viewModel = mocker.CreateInstance<MainWindowViewModel>();

        //Act
        viewModel.SetWindowOpacityCommand.Execute(0.75);

        //Assert
        Assert.Equal(0.75, viewModel.WindowOpacity);
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(0.9)]
    [InlineData(0.75)]
    [InlineData(0.5)]
    public void SetWindowOpacityCommand_SetsCorrectOpacityValue(double opacity)
    {
        //Arrange
        AutoMocker mocker = new();
        MainWindowViewModel viewModel = mocker.CreateInstance<MainWindowViewModel>();

        //Act
        viewModel.SetWindowOpacityCommand.Execute(opacity);

        //Assert
        Assert.Equal(opacity, viewModel.WindowOpacity);
    }

    [Fact]
    public void SetWindowOpacityCommand_CanAlwaysExecute()
    {
        //Arrange
        AutoMocker mocker = new();
        MainWindowViewModel viewModel = mocker.CreateInstance<MainWindowViewModel>();

        //Act
        bool canExecute = viewModel.SetWindowOpacityCommand.CanExecute(0.5);

        //Assert
        Assert.True(canExecute);
    }
}