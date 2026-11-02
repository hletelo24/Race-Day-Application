using System.ComponentModel.DataAnnotations;

namespace front_end_web.Models.Results;

public class RaceResultViewModel
{
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = "";
    public string ParticipantName { get; set; } = "";
    public string? Email { get; set; }
    public DateTime EnrolledAt { get; set; }
    public int? ResultId { get; set; }
    public string TimeText { get; set; } = "";
}

public class ParticipantRaceResultViewModel
{
    public int ResultsId { get; set; }
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = "";
    public DateTime EventDate { get; set; }
    public decimal Distance { get; set; }
    public string TimeText { get; set; } = "";
}
