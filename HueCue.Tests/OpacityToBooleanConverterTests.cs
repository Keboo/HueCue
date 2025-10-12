using System.Globalization;
using System.Windows.Data;

namespace HueCue.Tests;

public class OpacityToBooleanConverterTests
{
    [Theory]
    [InlineData(1.0, "1.0", true)]
    [InlineData(0.9, "0.9", true)]
    [InlineData(0.75, "0.75", true)]
    [InlineData(0.5, "0.5", true)]
    [InlineData(1.0, "0.9", false)]
    [InlineData(0.9, "1.0", false)]
    public void Convert_ReturnsCorrectBooleanValue(double value, string parameter, bool expected)
    {
        // Arrange
        var converter = new OpacityToBooleanConverter();

        // Act
        var result = converter.Convert(value, typeof(bool), parameter, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(expected, (bool)result);
    }

    [Fact]
    public void Convert_WithinTolerance_ReturnsTrue()
    {
        // Arrange
        var converter = new OpacityToBooleanConverter();
        double value = 0.900001; // Within 0.01 tolerance

        // Act
        var result = converter.Convert(value, typeof(bool), "0.9", CultureInfo.InvariantCulture);

        // Assert
        Assert.True((bool)result);
    }

    [Fact]
    public void Convert_OutsideTolerance_ReturnsFalse()
    {
        // Arrange
        var converter = new OpacityToBooleanConverter();
        double value = 0.92; // Outside 0.01 tolerance

        // Act
        var result = converter.Convert(value, typeof(bool), "0.9", CultureInfo.InvariantCulture);

        // Assert
        Assert.False((bool)result);
    }

    [Fact]
    public void Convert_InvalidParameter_ReturnsFalse()
    {
        // Arrange
        var converter = new OpacityToBooleanConverter();

        // Act
        var result = converter.Convert(1.0, typeof(bool), "invalid", CultureInfo.InvariantCulture);

        // Assert
        Assert.False((bool)result);
    }

    [Fact]
    public void ConvertBack_WhenTrueAndValidParameter_ReturnsOpacityValue()
    {
        // Arrange
        var converter = new OpacityToBooleanConverter();

        // Act
        var result = converter.ConvertBack(true, typeof(double), "0.75", CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(0.75, (double)result);
    }

    [Fact]
    public void ConvertBack_WhenFalse_ReturnsDoNothing()
    {
        // Arrange
        var converter = new OpacityToBooleanConverter();

        // Act
        var result = converter.ConvertBack(false, typeof(double), "0.75", CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(Binding.DoNothing, result);
    }

    [Fact]
    public void ConvertBack_InvalidParameter_ReturnsDoNothing()
    {
        // Arrange
        var converter = new OpacityToBooleanConverter();

        // Act
        var result = converter.ConvertBack(true, typeof(double), "invalid", CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(Binding.DoNothing, result);
    }
}