using System.ComponentModel.DataAnnotations;

namespace race_day_backend_api.Models
{
    public class Route
    {
        [Key]
        public int RouteId { get; set; }

        [Required]
        [MaxLength(150)]
        public string RouteName { get; set; } = string.Empty;

        [MaxLength(255)]
        public string Location { get; set; } = string.Empty;

        public decimal Distance { get; set; }

        public string? ImageUrl { get; set; }

        // Navigation
        public ICollection<Event> Events { get; set; }
            = new List<Event>();
    }
}
