namespace Server.Models;

public class Attendant
{
    public int Id { get; set; }
    public required string Email { get; set; }
    public string? Name { get; set; }
    
    // Navigation property
    public ICollection<MeetingAttendance> MeetingAttendances { get; set; } = new List<MeetingAttendance>();
}
