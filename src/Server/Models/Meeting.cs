namespace Server.Models;

public class Meeting
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public DateTime Date { get; set; }
    
    // Navigation properties
    public ICollection<MeetingAttendance> MeetingAttendances { get; set; } = new List<MeetingAttendance>();
    public int? SummaryId { get; set; }
    public Summary? Summary { get; set; }
}
