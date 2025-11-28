using System.Text;
using System.Text.Json;

namespace Server.Helpers;

/// <summary>
/// Static helper class for validating CSV files
/// </summary>
public static class FileValidator
{
    /// <summary>
    /// Validation result with detailed error information
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public string? DetectedType { get; set; }
        public string? OriginalExtension { get; set; }
    }

    /// <summary>
    /// Checks if the provided MIME type is valid for CSV files
    /// </summary>
    /// <param name="contentType">The MIME type to validate</param>
    /// <returns>True if the MIME type is valid for CSV files, false otherwise</returns>
    public static bool IsValidCsvMimeType(string contentType)
    {
        var validMimeTypes = new[] 
        { 
            "text/csv", 
            "application/csv",
            "text/plain",
            "application/vnd.ms-excel"
        };
        
        return validMimeTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Asynchronously validates if a stream contains valid CSV file content
    /// </summary>
    /// <param name="stream">The stream to validate</param>
    /// <returns>True if the stream contains valid CSV content, false otherwise</returns>
    public static async Task<bool> IsValidCsvFileAsync(Stream stream)
    {
        var result = await ValidateCsvFileAsync(stream, "unknown.csv");
        return result.IsValid;
    }

    /// <summary>
    /// Comprehensive validation of CSV file with detailed error reporting
    /// </summary>
    /// <param name="stream">The stream to validate</param>
    /// <param name="fileName">Original file name for extension detection</param>
    /// <returns>Detailed validation result</returns>
    public static async Task<ValidationResult> ValidateCsvFileAsync(Stream stream, string fileName)
    {
        if (stream == null || !stream.CanRead)
        {
            return new ValidationResult
            {
                IsValid = false,
                ErrorCode = "INVALID_STREAM",
                ErrorMessage = "Stream is null or not readable",
                OriginalExtension = Path.GetExtension(fileName)
            };
        }

        try
        {
            // Read the first chunk for analysis
            var buffer = new byte[8192]; // Read more bytes for better detection
            var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            
            // Reset stream position
            if (stream.CanSeek)
                stream.Position = 0;

            if (bytesRead == 0)
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorCode = "EMPTY_FILE",
                    ErrorMessage = "File is empty",
                    OriginalExtension = Path.GetExtension(fileName)
                };
            }

            var extension = Path.GetExtension(fileName);

            // Detect encoding and get text content for analysis
            var encoding = DetectEncoding(buffer, bytesRead);
            var bomLength = GetBomLength(buffer, encoding);
            string textContent;
            
            try
            {
                textContent = encoding.GetString(buffer, bomLength, bytesRead - bomLength);
            }
            catch
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorCode = "ENCODING_ERROR",
                    ErrorMessage = "Unable to decode file content",
                    DetectedType = "binary",
                    OriginalExtension = extension
                };
            }

            // Check for JSON masquerading as CSV
            if (IsJsonContent(textContent))
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorCode = "TYPE_MISMATCH",
                    ErrorMessage = "File appears to be JSON, not CSV. File extension does not match actual content type.",
                    DetectedType = "application/json",
                    OriginalExtension = extension
                };
            }

            // Check for other binary/non-CSV formats
            if (IsBinaryContent(buffer, bytesRead))
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorCode = "BINARY_CONTENT",
                    ErrorMessage = "File contains binary content, not valid CSV",
                    DetectedType = "binary",
                    OriginalExtension = extension
                };
            }

            // Check for HTML (before XML since HTML starts with < too)
            if (IsHtmlContent(textContent))
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorCode = "TYPE_MISMATCH",
                    ErrorMessage = "File appears to be HTML, not CSV",
                    DetectedType = "text/html",
                    OriginalExtension = extension
                };
            }

            // Check for XML
            if (IsXmlContent(textContent))
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorCode = "TYPE_MISMATCH",
                    ErrorMessage = "File appears to be XML, not CSV",
                    DetectedType = "text/xml",
                    OriginalExtension = extension
                };
            }

            // Validate CSV structure
            if (!HasCsvStructure(textContent))
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorCode = "INVALID_CSV_STRUCTURE",
                    ErrorMessage = "File does not appear to have valid CSV structure",
                    DetectedType = "text/plain",
                    OriginalExtension = extension
                };
            }

            return new ValidationResult
            {
                IsValid = true,
                DetectedType = "text/csv",
                OriginalExtension = extension
            };
        }
        catch (Exception ex)
        {
            return new ValidationResult
            {
                IsValid = false,
                ErrorCode = "VALIDATION_ERROR",
                ErrorMessage = $"Error during validation: {ex.Message}",
                OriginalExtension = Path.GetExtension(fileName)
            };
        }
    }

    private static Encoding DetectEncoding(byte[] buffer, int bytesRead)
    {
        if (bytesRead >= 2)
        {
            // Check for UTF-16 LE BOM
            if (buffer[0] == 0xFF && buffer[1] == 0xFE)
                return Encoding.Unicode;
            
            // Check for UTF-16 BE BOM
            if (buffer[0] == 0xFE && buffer[1] == 0xFF)
                return Encoding.BigEndianUnicode;
        }

        if (bytesRead >= 3)
        {
            // Check for UTF-8 BOM
            if (buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
                return Encoding.UTF8;
        }

        // Default to UTF-8
        return Encoding.UTF8;
    }

    private static int GetBomLength(byte[] buffer, Encoding encoding)
    {
        if (encoding == Encoding.UTF8 && buffer.Length >= 3 && 
            buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
            return 3;
        
        if ((encoding == Encoding.Unicode || encoding == Encoding.BigEndianUnicode) && 
            buffer.Length >= 2)
            return 2;

        return 0;
    }

    private static bool IsJsonContent(string content)
    {
        var trimmed = content.TrimStart();
        if (string.IsNullOrWhiteSpace(trimmed))
            return false;

        // Check for JSON object or array start
        if (trimmed.StartsWith("{") || trimmed.StartsWith("["))
        {
            // Try to parse as JSON to confirm (limit to first 4KB to avoid performance issues with large files)
            try
            {
                var sampleSize = Math.Min(content.Length, 4096);
                var sample = content.Substring(0, sampleSize);
                using var doc = JsonDocument.Parse(sample);
                return true;
            }
            catch (JsonException)
            {
                // Not valid JSON, continue with other checks
            }
        }

        return false;
    }

    private static bool IsXmlContent(string content)
    {
        var trimmed = content.TrimStart();
        return trimmed.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("<", StringComparison.Ordinal);
    }

    private static bool IsHtmlContent(string content)
    {
        var trimmed = content.TrimStart().ToLower();
        return trimmed.StartsWith("<!doctype html") ||
               trimmed.StartsWith("<html") ||
               trimmed.Contains("<html>") ||
               trimmed.Contains("<body>");
    }

    private static bool IsBinaryContent(byte[] buffer, int bytesRead)
    {
        // Check for common binary file signatures
        if (bytesRead >= 4)
        {
            // PDF
            if (buffer[0] == 0x25 && buffer[1] == 0x50 && buffer[2] == 0x44 && buffer[3] == 0x46)
                return true;

            // ZIP/Office files
            if (buffer[0] == 0x50 && buffer[1] == 0x4B && buffer[2] == 0x03 && buffer[3] == 0x04)
                return true;
        }

        // Check ASCII ratio
        var asciiCount = 0;
        var sampleSize = Math.Min(bytesRead, 512);
        
        for (int i = 0; i < sampleSize; i++)
        {
            var b = buffer[i];
            // Check for printable ASCII or common whitespace
            if ((b >= 32 && b <= 126) || b == 9 || b == 10 || b == 13)
            {
                asciiCount++;
            }
        }

        // If less than 80% ASCII, likely binary
        return (double)asciiCount / sampleSize < 0.8;
    }

    private static bool HasCsvStructure(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return false;

        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        
        // Need at least 1 line
        if (lines.Length < 1)
            return false;

        // Check for common CSV characteristics
        // Look for tabs, commas, or other delimiters
        var hasDelimiters = lines.Any(line => 
            line.Contains(',') || 
            line.Contains('\t') || 
            line.Contains(';'));

        return hasDelimiters;
    }
}
