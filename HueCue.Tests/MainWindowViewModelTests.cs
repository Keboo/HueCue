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
        Assert.Equal(HistogramOverlay.Below, viewModel.Overlay);
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
}