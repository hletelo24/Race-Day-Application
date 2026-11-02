namespace front_end_web.Models.Organiser;

public class OrganiserEnrolmentViewModel
{
    public int EnrolmentId { get; set; }

    public int EventId { get; set; }

    public string EventName { get; set; } = string.Empty;

    public string ParticipantName { get; set; } = string.Empty;
    public int ParticipantId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public string Status { get; set; } = "Pending";
}