using Xunit;

namespace HueCue.Tests;

public class AtemConnectionTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        //Arrange & Act
        using var connection = new AtemConnection();

        //Assert
        Assert.False(connection.IsConnected);
        Assert.Null(connection.PreviewInput);
        Assert.Null(connection.ProgramInput);
    }

    [Fact]
    public void Dispose_DoesNotThrowException()
    {
        //Arrange
        var connection = new AtemConnection();

        //Act & Assert
        var exception = Record.Exception(() => connection.Dispose());
        Assert.Null(exception);
    }

    [Fact]
    public void Disconnect_WhenNotConnected_DoesNotThrowException()
    {
        //Arrange
        using var connection = new AtemConnection();

        //Act & Assert
        var exception = Record.Exception(() => connection.Disconnect());
        Assert.Null(exception);
    }

    [Fact]
    public void Dispose_MultipleTimes_DoesNotThrowException()
    {
        //Arrange
        var connection = new AtemConnection();

        //Act & Assert
        connection.Dispose();
        var exception = Record.Exception(() => connection.Dispose());
        Assert.Null(exception);
    }
}
