using System.ComponentModel.DataAnnotations;

namespace front_end_web.Models.Organiser;

public class OrganiserResultViewModel
{
    public int EnrolmentId { get; set; }

    public int EventId { get; set; }

    public string EventName { get; set; } = string.Empty;

    public string ParticipantName { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Position { get; set; }

    [Required]
    public string FinishTime { get; set; } = string.Empty;

    public bool Published { get; set; }
}