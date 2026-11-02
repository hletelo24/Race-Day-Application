using System.ComponentModel.DataAnnotations;

namespace front_end_web.Models.Organiser;

public class OrganiserCategoryViewModel
{
    public int CategoryId { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Range(1, 120)]
    public int? MinimumAge { get; set; }

    [Range(1, 120)]
    public int? MaximumAge { get; set; }

    public int? EventId { get; set; }

    public string? EventName { get; set; }
}