namespace front_end_web.Models.Organiser;

public class OrganiserDashboardViewModel
{
    public string OrganiserName { get; set; } = string.Empty;

    public int TotalEvents { get; set; }

    public int UpcomingEvents { get; set; }

    public int TotalEnrolments { get; set; }

    public List<OrganiserEventViewModel> Events { get; set; } = new();

    public string? ErrorMessage { get; set; }
}