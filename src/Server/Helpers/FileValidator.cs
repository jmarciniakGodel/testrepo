using System.Text;
using System.Text.Json;

namespace Server.Helpers;

/// <summary>
/// Static helper class for validating CSV files
/// </summary>
public static class FileValidator
{
    // Constants for buffer sizes and thresholds
    private const int BufferSize = 8192;
    private const int SampleSizeForJson = 4096;
    private const int SampleSizeForBinary = 512;
    private const double BinaryContentThreshold = 0.8;

    // Error codes
    private const string ErrorCodeInvalidStream = "INVALID_STREAM";
    private const string ErrorCodeEmptyFile = "EMPTY_FILE";
    private const string ErrorCodeEncodingError = "ENCODING_ERROR";
    private const string ErrorCodeTypeMismatch = "TYPE_MISMATCH";
    private const string ErrorCodeBinaryContent = "BINARY_CONTENT";
    private const string ErrorCodeInvalidCsvStructure = "INVALID_CSV_STRUCTURE";
    private const string ErrorCodeValidationError = "VALIDATION_ERROR";

    // Error messages
    private const string ErrorMessageInvalidStream = "Stream is null or not readable";
    private const string ErrorMessageEmptyFile = "File is empty";
    private const string ErrorMessageEncodingError = "Unable to decode file content";
    private const string ErrorMessageJsonContent = "File appears to be JSON, not CSV. File extension does not match actual content type.";
    private const string ErrorMessageBinaryContent = "File contains binary content, not valid CSV";
    private const string ErrorMessageHtmlContent = "File appears to be HTML, not CSV";
    private const string ErrorMessageXmlContent = "File appears to be XML, not CSV";
    private const string ErrorMessageInvalidCsvStructure = "File does not appear to have valid CSV structure";

    // Detected types
    private const string DetectedTypeBinary = "binary";
    private const string DetectedTypeJson = "application/json";
    private const string DetectedTypeHtml = "text/html";
    private const string DetectedTypeXml = "text/xml";
    private const string DetectedTypePlainText = "text/plain";
    private const string DetectedTypeCsv = "text/csv";

    // UTF-8 BOM bytes
    private const byte Utf8Bom1 = 0xEF;
    private const byte Utf8Bom2 = 0xBB;
    private const byte Utf8Bom3 = 0xBF;

    // UTF-16 BOM bytes
    private const byte Utf16LeBom1 = 0xFF;
    private const byte Utf16LeBom2 = 0xFE;
    private const byte Utf16BeBom1 = 0xFE;
    private const byte Utf16BeBom2 = 0xFF;

    // PDF signature
    private const byte PdfSignature1 = 0x25;
    private const byte PdfSignature2 = 0x50;
    private const byte PdfSignature3 = 0x44;
    private const byte PdfSignature4 = 0x46;

    // ZIP/Office signature
    private const byte ZipSignature1 = 0x50;
    private const byte ZipSignature2 = 0x4B;
    private const byte ZipSignature3 = 0x03;
    private const byte ZipSignature4 = 0x04;

    // ASCII character ranges
    private const int AsciiPrintableStart = 32;
    private const int AsciiPrintableEnd = 126;
    private const int AsciiTab = 9;
    private const int AsciiLineFeed = 10;
    private const int AsciiCarriageReturn = 13;
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
            return CreateValidationResult(false, ErrorCodeInvalidStream, ErrorMessageInvalidStream, null, fileName);
        }

        try
        {
            var buffer = new byte[BufferSize];
            var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            
            ResetStreamPosition(stream);

            if (bytesRead == 0)
            {
                return CreateValidationResult(false, ErrorCodeEmptyFile, ErrorMessageEmptyFile, null, fileName);
            }

            var extension = Path.GetExtension(fileName);
            var encoding = DetectEncoding(buffer, bytesRead);
            var bomLength = GetBomLength(buffer, encoding);
            var hasBom = bomLength > 0;

            if (!TryDecodeContent(buffer, bytesRead, bomLength, encoding, out var textContent))
            {
                return CreateValidationResult(false, ErrorCodeEncodingError, ErrorMessageEncodingError, DetectedTypeBinary, fileName);
            }

            // Perform content type checks
            var contentValidationResult = ValidateContentType(textContent, hasBom, buffer, bytesRead, extension);
            if (!contentValidationResult.IsValid)
            {
                return contentValidationResult;
            }

            return CreateValidationResult(true, null, null, DetectedTypeCsv, fileName);
        }
        catch (Exception ex)
        {
            return CreateValidationResult(false, ErrorCodeValidationError, $"Error during validation: {ex.Message}", null, fileName);
        }
    }

    /// <summary>
    /// Creates a validation result with the specified parameters
    /// </summary>
    private static ValidationResult CreateValidationResult(bool isValid, string? errorCode, string? errorMessage, string? detectedType, string fileName)
    {
        return new ValidationResult
        {
            IsValid = isValid,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            DetectedType = detectedType,
            OriginalExtension = Path.GetExtension(fileName)
        };
    }

    /// <summary>
    /// Resets stream position if seeking is supported
    /// </summary>
    private static void ResetStreamPosition(Stream stream)
    {
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }
    }

    /// <summary>
    /// Attempts to decode buffer content to text
    /// </summary>
    private static bool TryDecodeContent(byte[] buffer, int bytesRead, int bomLength, Encoding encoding, out string textContent)
    {
        try
        {
            textContent = encoding.GetString(buffer, bomLength, bytesRead - bomLength);
            return true;
        }
        catch
        {
            textContent = string.Empty;
            return false;
        }
    }

    /// <summary>
    /// Validates the content type of the file
    /// </summary>
    private static ValidationResult ValidateContentType(string textContent, bool hasBom, byte[] buffer, int bytesRead, string extension)
    {
        // Check for JSON
        if (IsJsonContent(textContent))
        {
            return new ValidationResult
            {
                IsValid = false,
                ErrorCode = ErrorCodeTypeMismatch,
                ErrorMessage = ErrorMessageJsonContent,
                DetectedType = DetectedTypeJson,
                OriginalExtension = extension
            };
        }

        // Check for binary content (skip if file has text BOM)
        if (!hasBom && IsBinaryContent(buffer, bytesRead))
        {
            return new ValidationResult
            {
                IsValid = false,
                ErrorCode = ErrorCodeBinaryContent,
                ErrorMessage = ErrorMessageBinaryContent,
                DetectedType = DetectedTypeBinary,
                OriginalExtension = extension
            };
        }

        // Check for HTML
        if (IsHtmlContent(textContent))
        {
            return new ValidationResult
            {
                IsValid = false,
                ErrorCode = ErrorCodeTypeMismatch,
                ErrorMessage = ErrorMessageHtmlContent,
                DetectedType = DetectedTypeHtml,
                OriginalExtension = extension
            };
        }

        // Check for XML
        if (IsXmlContent(textContent))
        {
            return new ValidationResult
            {
                IsValid = false,
                ErrorCode = ErrorCodeTypeMismatch,
                ErrorMessage = ErrorMessageXmlContent,
                DetectedType = DetectedTypeXml,
                OriginalExtension = extension
            };
        }

        // Validate CSV structure
        if (!HasCsvStructure(textContent))
        {
            return new ValidationResult
            {
                IsValid = false,
                ErrorCode = ErrorCodeInvalidCsvStructure,
                ErrorMessage = ErrorMessageInvalidCsvStructure,
                DetectedType = DetectedTypePlainText,
                OriginalExtension = extension
            };
        }

        return new ValidationResult
        {
            IsValid = true,
            DetectedType = DetectedTypeCsv,
            OriginalExtension = extension
        };
    }

    private static Encoding DetectEncoding(byte[] buffer, int bytesRead)
    {
        if (bytesRead >= 2)
        {
            // Check for UTF-16 LE BOM
            if (buffer[0] == Utf16LeBom1 && buffer[1] == Utf16LeBom2)
                return Encoding.Unicode;
            
            // Check for UTF-16 BE BOM
            if (buffer[0] == Utf16BeBom1 && buffer[1] == Utf16BeBom2)
                return Encoding.BigEndianUnicode;
        }

        if (bytesRead >= 3)
        {
            // Check for UTF-8 BOM
            if (buffer[0] == Utf8Bom1 && buffer[1] == Utf8Bom2 && buffer[2] == Utf8Bom3)
                return Encoding.UTF8;
        }

        // Default to UTF-8
        return Encoding.UTF8;
    }

    private static int GetBomLength(byte[] buffer, Encoding encoding)
    {
        if (encoding == Encoding.UTF8 && buffer.Length >= 3 && 
            buffer[0] == Utf8Bom1 && buffer[1] == Utf8Bom2 && buffer[2] == Utf8Bom3)
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
        if (!trimmed.StartsWith("{") && !trimmed.StartsWith("["))
            return false;

        // Try to parse as JSON to confirm (limit to first 4KB to avoid performance issues)
        try
        {
            var sampleSize = Math.Min(content.Length, SampleSizeForJson);
            var sample = content.Substring(0, sampleSize);
            using var doc = JsonDocument.Parse(sample);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
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
        if (IsPdfFile(buffer, bytesRead) || IsZipFile(buffer, bytesRead))
        {
            return true;
        }

        // Check ASCII ratio
        return !HasSufficientAsciiContent(buffer, bytesRead);
    }

    /// <summary>
    /// Checks if the buffer contains PDF file signature
    /// </summary>
    private static bool IsPdfFile(byte[] buffer, int bytesRead)
    {
        return bytesRead >= 4 && 
               buffer[0] == PdfSignature1 && 
               buffer[1] == PdfSignature2 && 
               buffer[2] == PdfSignature3 && 
               buffer[3] == PdfSignature4;
    }

    /// <summary>
    /// Checks if the buffer contains ZIP/Office file signature
    /// </summary>
    private static bool IsZipFile(byte[] buffer, int bytesRead)
    {
        return bytesRead >= 4 && 
               buffer[0] == ZipSignature1 && 
               buffer[1] == ZipSignature2 && 
               buffer[2] == ZipSignature3 && 
               buffer[3] == ZipSignature4;
    }

    /// <summary>
    /// Checks if buffer has sufficient ASCII content to be considered text
    /// </summary>
    private static bool HasSufficientAsciiContent(byte[] buffer, int bytesRead)
    {
        var asciiCount = 0;
        var sampleSize = Math.Min(bytesRead, SampleSizeForBinary);
        
        for (int i = 0; i < sampleSize; i++)
        {
            var b = buffer[i];
            // Check for printable ASCII or common whitespace
            if (IsAsciiCharacter(b))
            {
                asciiCount++;
            }
        }

        // If less than 80% ASCII, likely binary
        return (double)asciiCount / sampleSize >= BinaryContentThreshold;
    }

    /// <summary>
    /// Checks if byte is a printable ASCII character or common whitespace
    /// </summary>
    private static bool IsAsciiCharacter(byte b)
    {
        return (b >= AsciiPrintableStart && b <= AsciiPrintableEnd) || 
               b == AsciiTab || 
               b == AsciiLineFeed || 
               b == AsciiCarriageReturn;
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
