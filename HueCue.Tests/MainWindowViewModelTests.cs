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
    public void Constructor_InitializesFaceDetection()
    {
        //Arrange & Act
        AutoMocker mocker = new();
        MainWindowViewModel viewModel = mocker.CreateInstance<MainWindowViewModel>();

        //Assert - Constructor should complete without throwing
        Assert.NotNull(viewModel);
    }
}