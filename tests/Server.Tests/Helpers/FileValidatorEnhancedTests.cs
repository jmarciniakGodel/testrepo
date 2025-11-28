using Server.Helpers;
using System.Text;

namespace Server.Tests.Helpers;

public class FileValidatorEnhancedTests
{
    [Fact]
    public async Task ValidateCsvFileAsync_JsonContent_ReturnsTypeMismatch()
    {
        // Arrange
        var jsonContent = @"{
  ""summaryId"": 1,
  ""htmlTable"": ""<table>...</table>""
}";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonContent));

        // Act
        var result = await FileValidator.ValidateCsvFileAsync(stream, "response.csv");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("TYPE_MISMATCH", result.ErrorCode);
        Assert.Contains("JSON", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateCsvFileAsync_ValidCsv_ReturnsSuccess()
    {
        // Arrange
        var csvContent = @"Team Meeting,2024-01-15
Name,Email,Duration
John Doe,john@example.com,45
Jane Smith,jane@example.com,30";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await FileValidator.ValidateCsvFileAsync(stream, "meeting.csv");

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal("text/csv", result.DetectedType);
    }

    [Fact]
    public async Task ValidateCsvFileAsync_EmptyFile_ReturnsEmptyFileError()
    {
        // Arrange
        var stream = new MemoryStream();

        // Act
        var result = await FileValidator.ValidateCsvFileAsync(stream, "empty.csv");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("EMPTY_FILE", result.ErrorCode);
    }

    [Fact]
    public async Task ValidateCsvFileAsync_XmlContent_ReturnsTypeMismatch()
    {
        // Arrange
        var xmlContent = "<?xml version=\"1.0\"?><root></root>";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xmlContent));

        // Act
        var result = await FileValidator.ValidateCsvFileAsync(stream, "file.csv");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("TYPE_MISMATCH", result.ErrorCode);
        Assert.Contains("XML", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateCsvFileAsync_HtmlContent_ReturnsTypeMismatch()
    {
        // Arrange
        var htmlContent = "<html><body><table></table></body></html>";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(htmlContent));

        // Act
        var result = await FileValidator.ValidateCsvFileAsync(stream, "file.csv");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("TYPE_MISMATCH", result.ErrorCode);
        Assert.Contains("HTML", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateCsvFileAsync_BinaryContent_ReturnsBinaryError()
    {
        // Arrange - PDF signature
        var binaryContent = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 };
        var stream = new MemoryStream(binaryContent);

        // Act
        var result = await FileValidator.ValidateCsvFileAsync(stream, "file.csv");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("BINARY_CONTENT", result.ErrorCode);
    }

    [Fact]
    public async Task ValidateCsvFileAsync_Utf16WithBom_ReturnsSuccess()
    {
        // Arrange - UTF-16 LE BOM followed by simple text
        var utf16Text = "Name\tEmail\tDuration\r\nJohn Doe\tjohn@example.com\t45m\r\n";
        var encoding = Encoding.Unicode; // UTF-16 LE
        var preamble = encoding.GetPreamble(); // BOM
        var contentBytes = encoding.GetBytes(utf16Text);
        var bytes = new byte[preamble.Length + contentBytes.Length];
        Buffer.BlockCopy(preamble, 0, bytes, 0, preamble.Length);
        Buffer.BlockCopy(contentBytes, 0, bytes, preamble.Length, contentBytes.Length);
        
        var stream = new MemoryStream(bytes);

        // Act
        var result = await FileValidator.ValidateCsvFileAsync(stream, "meeting.csv");

        // Assert
        Assert.True(result.IsValid, $"Expected valid but got: {result.ErrorCode} - {result.ErrorMessage}");
        Assert.Equal("text/csv", result.DetectedType);
    }
}
