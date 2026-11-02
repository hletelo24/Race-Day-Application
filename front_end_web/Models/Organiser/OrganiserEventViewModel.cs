namespace front_end_web.Models.Organiser;

public class OrganiserEventViewModel
{
    public int EventId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public string Location { get; set; } = string.Empty;

    public decimal Distance { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string? BannerImageUrl { get; set; }

    public int EnrolmentCount { get; set; }

    public string Status { get; set; } = "Upcoming";
}