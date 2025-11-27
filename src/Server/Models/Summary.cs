namespace Server.Models;

public class Summary
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public required string HtmlTable { get; set; }
    public required byte[] XlsxData { get; set; }
    
    // Navigation property
    public ICollection<Meeting> Meetings { get; set; } = new List<Meeting>();
}
