namespace Server.Helpers;

public static class FileValidator
{
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

    public static async Task<bool> IsValidCsvFileAsync(Stream stream)
    {
        if (stream == null || !stream.CanRead)
            return false;

        try
        {
            // Read the first few bytes to check file signature
            var buffer = new byte[512];
            var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            
            // Reset stream position
            if (stream.CanSeek)
                stream.Position = 0;

            if (bytesRead == 0)
                return false;

            // Check for UTF-16 BOM (either Little Endian or Big Endian)
            if (bytesRead >= 2)
            {
                if ((buffer[0] == 0xFF && buffer[1] == 0xFE) || // UTF-16 LE BOM
                    (buffer[0] == 0xFE && buffer[1] == 0xFF))   // UTF-16 BE BOM
                {
                    // This is a UTF-16 encoded file, which is valid for CSV
                    return true;
                }
            }

            // Check for UTF-8 BOM
            if (bytesRead >= 3 && buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
            {
                // UTF-8 with BOM is valid
                return true;
            }

            // Check if it contains printable ASCII characters typical of CSV
            // CSV files should contain mostly ASCII printable characters
            var asciiCount = 0;
            var sampleSize = Math.Min(bytesRead, 100);
            
            for (int i = 0; i < sampleSize; i++)
            {
                var b = buffer[i];
                // Check for printable ASCII or common whitespace
                if ((b >= 32 && b <= 126) || b == 9 || b == 10 || b == 13)
                {
                    asciiCount++;
                }
            }

            // At least 80% should be valid ASCII characters
            return (double)asciiCount / sampleSize >= 0.8;
        }
        catch
        {
            return false;
        }
    }
}
