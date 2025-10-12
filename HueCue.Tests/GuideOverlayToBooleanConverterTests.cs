using System.Globalization;

namespace HueCue.Tests;

public class GuideOverlayToBooleanConverterTests
{
    [Fact]
    public void Convert_ReturnsTrue_WhenValueMatchesParameter()
    {
        // Arrange
        var converter = new GuideOverlayToBooleanConverter();

        // Act
        var result = converter.Convert(GuideOverlay.RuleOfThirds, typeof(bool), GuideOverlay.RuleOfThirds, CultureInfo.InvariantCulture);

        // Assert
        Assert.True((bool)result);
    }

    [Fact]
    public void Convert_ReturnsFalse_WhenValueDoesNotMatchParameter()
    {
        // Arrange
        var converter = new GuideOverlayToBooleanConverter();

        // Act
        var result = converter.Convert(GuideOverlay.None, typeof(bool), GuideOverlay.RuleOfThirds, CultureInfo.InvariantCulture);

        // Assert
        Assert.False((bool)result);
    }

    [Fact]
    public void Convert_ReturnsFalse_WhenValueIsNotGuideOverlay()
    {
        // Arrange
        var converter = new GuideOverlayToBooleanConverter();

        // Act
        var result = converter.Convert("invalid", typeof(bool), GuideOverlay.RuleOfThirds, CultureInfo.InvariantCulture);

        // Assert
        Assert.False((bool)result);
    }

    [Fact]
    public void ConvertBack_ReturnsParameter_WhenValueIsTrue()
    {
        // Arrange
        var converter = new GuideOverlayToBooleanConverter();

        // Act
        var result = converter.ConvertBack(true, typeof(GuideOverlay), GuideOverlay.RuleOfThirds, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(GuideOverlay.RuleOfThirds, result);
    }

    [Fact]
    public void ConvertBack_ReturnsNone_WhenValueIsFalse()
    {
        // Arrange
        var converter = new GuideOverlayToBooleanConverter();

        // Act
        var result = converter.ConvertBack(false, typeof(GuideOverlay), GuideOverlay.RuleOfThirds, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(GuideOverlay.None, result);
    }

    [Fact]
    public void Convert_ReturnsTrue_WhenHeatMapValueMatchesParameter()
    {
        // Arrange
        var converter = new GuideOverlayToBooleanConverter();

        // Act
        var result = converter.Convert(GuideOverlay.HeatMap, typeof(bool), GuideOverlay.HeatMap, CultureInfo.InvariantCulture);

        // Assert
        Assert.True((bool)result);
    }

    [Fact]
    public void ConvertBack_ReturnsHeatMap_WhenValueIsTrue()
    {
        // Arrange
        var converter = new GuideOverlayToBooleanConverter();

        // Act
        var result = converter.ConvertBack(true, typeof(GuideOverlay), GuideOverlay.HeatMap, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(GuideOverlay.HeatMap, result);
    }
}