using System.ComponentModel.DataAnnotations;

namespace front_end_web.Models.Events;

public class EventViewModel
{
    public int EventId { get; set; }

    // Set by the API from the authenticated organiser's JWT.
    public int UserId { get; set; }

    [Required(ErrorMessage = "Select a category.")]
    [Range(1, int.MaxValue, ErrorMessage = "Select a valid category.")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Select a route.")]
    [Range(1, int.MaxValue, ErrorMessage = "Select a valid route.")]
    public int RouteId { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [StringLength(255)]
    [Required]
    [Display(Name = "Start location")]
    public string StartLocation { get; set; } = string.Empty;

    [StringLength(255)]
    [Required]
    [Display(Name = "End location")]
    public string EndLocation { get; set; } = string.Empty;

    [Display(Name = "Start time")]
    public TimeSpan StartTime { get; set; }

    [Range(typeof(decimal), "1", "1000")]
    public decimal Distance { get; set; }

    [Required]
    [Display(Name = "Event date")]
    public DateTime Date { get; set; } = DateTime.Today.AddDays(7);

    public string? ImageUrl { get; set; }
    public IFormFile? BannerImage { get; set; }

    // Optional display-only fields populated by API DTOs, if available.
    public string? CategoryTitle { get; set; }
    public string? RouteName { get; set; }
}

public class CategoryOptionViewModel
{
    public int CategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class RouteOptionViewModel
{
    public int RouteId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Name { get; set; }
    public decimal DistanceKm { get; set; }
}

public class EnrolEventViewModel
{
    [Range(1, int.MaxValue)]
    public int EventId { get; set; }
}

public class ParticipantEnrolmentViewModel
{
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public DateTime? EventDate { get; set; }
    public TimeSpan? StartTime { get; set; }
    public string? StartLocation { get; set; }
    public string? EndLocation { get; set; }
    public decimal? Distance { get; set; }
    public string? CategoryTitle { get; set; }
}
