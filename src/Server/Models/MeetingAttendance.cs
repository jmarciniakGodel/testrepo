namespace Server.Models;

public class MeetingAttendance
{
    public int Id { get; set; }
    public int MeetingId { get; set; }
    public Meeting Meeting { get; set; } = null!;
    
    public int AttendantId { get; set; }
    public Attendant Attendant { get; set; } = null!;
    
    public TimeSpan Duration { get; set; }
}
