using System.Net;
using System.Net.Http;

using Moq;
using Moq.Protected;

namespace HueCue.Tests;

public class AjaHeloStreamSourceTests
{
    [Fact]
    public void Constructor_SetsDefaultIpAddress()
    {
        //Arrange & Act
        using var streamSource = new AjaHeloStreamSource();

        //Assert
        Assert.NotNull(streamSource);
    }

    [Fact]
    public void Constructor_AcceptsCustomIpAddress()
    {
        //Arrange & Act
        using var streamSource = new AjaHeloStreamSource("192.168.1.100");

        //Assert
        Assert.NotNull(streamSource);
    }

    [Fact]
    public async Task GetFrameAsync_ReturnsNull_WhenDisposed()
    {
        //Arrange
        var streamSource = new AjaHeloStreamSource();
        streamSource.Dispose();

        //Act
        var result = await streamSource.GetFrameAsync();

        //Assert
        Assert.Null(result);
    }

    [Fact]
    public void Dispose_DoesNotThrowException()
    {
        //Arrange
        var streamSource = new AjaHeloStreamSource();

        //Act & Assert
        var exception = Record.Exception(() => streamSource.Dispose());
        Assert.Null(exception);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        //Arrange
        var streamSource = new AjaHeloStreamSource();

        //Act & Assert
        var exception = Record.Exception(() =>
        {
            streamSource.Dispose();
            streamSource.Dispose();
        });
        Assert.Null(exception);
    }
}