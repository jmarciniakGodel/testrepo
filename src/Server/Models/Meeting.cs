namespace Server.Models;

/// <summary>
/// Represents a meeting with attendees
/// </summary>
public class Meeting
{
    /// <summary>
    /// Gets or sets the unique identifier for the meeting
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the title of the meeting
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Gets or sets the date when the meeting occurred
    /// </summary>
    public DateTime Date { get; set; }
    
    /// <summary>
    /// Gets or sets the collection of attendances for this meeting
    /// </summary>
    public ICollection<MeetingAttendance> MeetingAttendances { get; set; } = new List<MeetingAttendance>();

    /// <summary>
    /// Gets or sets the identifier of the summary this meeting belongs to
    /// </summary>
    public int? SummaryId { get; set; }

    /// <summary>
    /// Gets or sets the summary this meeting belongs to
    /// </summary>
    public Summary? Summary { get; set; }
}
