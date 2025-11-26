using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using System.Text;

namespace CsvToExcelApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CsvToExcelController : ControllerBase
{
    private readonly ILogger<CsvToExcelController> _logger;

    public CsvToExcelController(ILogger<CsvToExcelController> logger)
    {
        _logger = logger;
    }

    [HttpPost("convert")]
    public async Task<IActionResult> ConvertCsvToExcel([FromForm] List<IFormFile> files)
    {
        if (files == null || files.Count == 0)
        {
            return BadRequest("No files uploaded.");
        }

        // Set EPPlus license context
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        try
        {
            using var package = new ExcelPackage();

            // Summary worksheet
            var summarySheet = package.Workbook.Worksheets.Add("Summary");
            summarySheet.Cells[1, 1].Value = "File Name";
            summarySheet.Cells[1, 2].Value = "Total Rows";
            summarySheet.Cells[1, 3].Value = "Total Columns";
            
            // Style header
            using (var range = summarySheet.Cells[1, 1, 1, 3])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            int summaryRow = 2;

            // Process each CSV file
            foreach (var file in files)
            {
                if (file.Length == 0)
                    continue;

                var fileName = Path.GetFileNameWithoutExtension(file.FileName);
                
                using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8);
                var csvContent = await reader.ReadToEndAsync();
                
                // Parse CSV
                var lines = csvContent.Split('\n')
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .Select(line => line.Trim())
                    .ToList();

                if (lines.Count == 0)
                    continue;

                // Create worksheet for this file
                var worksheet = package.Workbook.Worksheets.Add(fileName);
                
                int rowIndex = 1;
                int maxColumns = 0;

                foreach (var line in lines)
                {
                    var values = ParseCsvLine(line);
                    maxColumns = Math.Max(maxColumns, values.Count);

                    for (int colIndex = 0; colIndex < values.Count; colIndex++)
                    {
                        worksheet.Cells[rowIndex, colIndex + 1].Value = values[colIndex];
                    }

                    // Style header row
                    if (rowIndex == 1)
                    {
                        using (var range = worksheet.Cells[1, 1, 1, values.Count])
                        {
                            range.Style.Font.Bold = true;
                            range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                        }
                    }

                    rowIndex++;
                }

                // Auto-fit columns
                worksheet.Cells.AutoFitColumns();

                // Add to summary
                summarySheet.Cells[summaryRow, 1].Value = file.FileName;
                summarySheet.Cells[summaryRow, 2].Value = lines.Count - 1; // Exclude header
                summarySheet.Cells[summaryRow, 3].Value = maxColumns;
                summaryRow++;
            }

            // Auto-fit summary columns
            summarySheet.Cells.AutoFitColumns();

            // Generate Excel file
            var excelBytes = package.GetAsByteArray();
            
            return File(excelBytes, 
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                "summary.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing CSV files");
            return StatusCode(500, $"Error processing files: {ex.Message}");
        }
    }

    private List<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var currentValue = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    // Escaped quote
                    currentValue.Append('"');
                    i++;
                }
                else
                {
                    // Toggle quote state
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                // End of value
                values.Add(currentValue.ToString());
                currentValue.Clear();
            }
            else
            {
                currentValue.Append(c);
            }
        }

        // Add last value
        values.Add(currentValue.ToString());

        return values;
    }
}
