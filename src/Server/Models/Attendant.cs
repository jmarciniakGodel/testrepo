namespace Server.Models;

/// <summary>
/// Represents an attendant who participates in meetings
/// </summary>
public class Attendant
{
    /// <summary>
    /// Gets or sets the unique identifier for the attendant
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the email address of the attendant
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// Gets or sets the name of the attendant
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// Gets or sets the collection of meeting attendances for this attendant
    /// </summary>
    public ICollection<MeetingAttendance> MeetingAttendances { get; set; } = new List<MeetingAttendance>();
}
