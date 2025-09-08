namespace HueCue.Tests;

public class BoolToPlayPauseTextConverterTests
{
    [Fact]
    public void Convert_WithTrueValue_ReturnsPause()
    {
        //Arrange
        var converter = new BoolToPlayPauseTextConverter();

        //Act
        var result = converter.Convert(true, typeof(string), null, System.Globalization.CultureInfo.InvariantCulture);

        //Assert
        Assert.Equal("_Pause", result);
    }

    [Fact]
    public void Convert_WithFalseValue_ReturnsPlay()
    {
        //Arrange
        var converter = new BoolToPlayPauseTextConverter();

        //Act
        var result = converter.Convert(false, typeof(string), null, System.Globalization.CultureInfo.InvariantCulture);

        //Assert
        Assert.Equal("_Play", result);
    }

    [Fact]
    public void Convert_WithNonBoolValue_ReturnsPlay()
    {
        //Arrange
        var converter = new BoolToPlayPauseTextConverter();

        //Act
        var result = converter.Convert("not a bool", typeof(string), null, System.Globalization.CultureInfo.InvariantCulture);

        //Assert
        Assert.Equal("_Play", result);
    }

    [Fact]
    public void ConvertBack_ThrowsNotImplementedException()
    {
        //Arrange
        var converter = new BoolToPlayPauseTextConverter();

        //Act & Assert
        Assert.Throws<NotImplementedException>(() =>
            converter.ConvertBack("_Play", typeof(bool), null, System.Globalization.CultureInfo.InvariantCulture));
    }
}