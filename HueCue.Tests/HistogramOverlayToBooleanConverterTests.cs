using System.Globalization;

namespace HueCue.Tests;

public class HistogramOverlayToBooleanConverterTests
{
    [Fact]
    public void Convert_ReturnsTrue_WhenOverlayMatchesParameter()
    {
        // Arrange
        var converter = new HistogramOverlayToBooleanConverter();

        // Act
        var result = converter.Convert(HistogramOverlay.Below, typeof(bool), HistogramOverlay.Below, CultureInfo.InvariantCulture);

        // Assert
        Assert.True((bool)result);
    }

    [Fact]
    public void Convert_ReturnsFalse_WhenOverlayDoesNotMatchParameter()
    {
        // Arrange
        var converter = new HistogramOverlayToBooleanConverter();

        // Act
        var result = converter.Convert(HistogramOverlay.Below, typeof(bool), HistogramOverlay.Right, CultureInfo.InvariantCulture);

        // Assert
        Assert.False((bool)result);
    }

    [Fact]
    public void ConvertBack_ReturnsParameter_WhenValueIsTrue()
    {
        // Arrange
        var converter = new HistogramOverlayToBooleanConverter();

        // Act
        var result = converter.ConvertBack(true, typeof(HistogramOverlay), HistogramOverlay.Right, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(HistogramOverlay.Right, result);
    }

    [Fact]
    public void ConvertBack_ReturnsBelow_WhenValueIsFalse()
    {
        // Arrange
        var converter = new HistogramOverlayToBooleanConverter();

        // Act
        var result = converter.ConvertBack(false, typeof(HistogramOverlay), HistogramOverlay.Right, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(HistogramOverlay.Below, result);
    }
}