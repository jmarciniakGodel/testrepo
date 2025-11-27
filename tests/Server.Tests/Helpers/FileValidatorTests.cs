using Server.Helpers;

namespace Server.Tests.Helpers;

public class FileValidatorTests
{
    [Theory]
    [InlineData("text/csv", true)]
    [InlineData("application/csv", true)]
    [InlineData("text/plain", true)]
    [InlineData("application/vnd.ms-excel", true)]
    [InlineData("application/json", false)]
    [InlineData("text/html", false)]
    public void IsValidCsvMimeType_ValidatesCorrectly(string mimeType, bool expected)
    {
        // Act
        var result = FileValidator.IsValidCsvMimeType(mimeType);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task IsValidCsvFileAsync_ValidCsvContent_ReturnsTrue()
    {
        // Arrange
        var csvContent = "Name,Email,Duration\nJohn Doe,john@example.com,45\n";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await FileValidator.IsValidCsvFileAsync(stream);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsValidCsvFileAsync_EmptyFile_ReturnsFalse()
    {
        // Arrange
        var stream = new MemoryStream();

        // Act
        var result = await FileValidator.IsValidCsvFileAsync(stream);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsValidCsvFileAsync_BinaryContent_ReturnsFalse()
    {
        // Arrange
        var binaryContent = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07 };
        var stream = new MemoryStream(binaryContent);

        // Act
        var result = await FileValidator.IsValidCsvFileAsync(stream);

        // Assert
        Assert.False(result);
    }
}
