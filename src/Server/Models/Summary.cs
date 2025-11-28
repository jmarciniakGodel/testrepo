namespace Server.Models;

/// <summary>
/// Represents a summary report of meeting attendance data
/// </summary>
public class Summary
{
    /// <summary>
    /// Gets or sets the unique identifier for the summary
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the summary was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the HTML table representation of the summary
    /// </summary>
    public required string HtmlTable { get; set; }

    /// <summary>
    /// Gets or sets the Excel file data for the summary
    /// </summary>
    public required byte[] XlsxData { get; set; }
    
    /// <summary>
    /// Gets or sets the collection of meetings included in this summary
    /// </summary>
    public ICollection<Meeting> Meetings { get; set; } = new List<Meeting>();
}
