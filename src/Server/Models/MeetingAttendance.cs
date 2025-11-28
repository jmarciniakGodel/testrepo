namespace Server.Models;

/// <summary>
/// Represents the attendance of an attendant at a specific meeting
/// </summary>
public class MeetingAttendance
{
    /// <summary>
    /// Gets or sets the unique identifier for the attendance record
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the meeting identifier
    /// </summary>
    public int MeetingId { get; set; }

    /// <summary>
    /// Gets or sets the meeting this attendance belongs to
    /// </summary>
    public Meeting Meeting { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the attendant identifier
    /// </summary>
    public int AttendantId { get; set; }

    /// <summary>
    /// Gets or sets the attendant for this attendance record
    /// </summary>
    public Attendant Attendant { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the duration of attendance in the meeting
    /// </summary>
    public TimeSpan Duration { get; set; }
}
