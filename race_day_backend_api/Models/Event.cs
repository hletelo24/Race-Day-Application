using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace race_day_backend_api.Models
{
    public class Event
    {
        [Key]
        public int EventId { get; set; }

        // Organiser/User
        [Required]
        public int UserId { get; set; }

        // Category
        [Required]
        public int CategoryId { get; set; }

        // Route
        [Required]
        public int RouteId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [MaxLength(255)]
        public string StartLocation { get; set; } = string.Empty;

        [MaxLength(255)]
        public string EndLocation { get; set; } = string.Empty;

        public TimeSpan StartTime { get; set; }

        public decimal Distance { get; set; }

        public DateTime Date { get; set; }

        public string? ImageUrl { get; set; }

        // Navigation properties

        // Event belongs to an organiser
        [ForeignKey(nameof(UserId))]
        public Organiser? Organiser { get; set; }

        // Event belongs to a category
        [ForeignKey(nameof(CategoryId))]
        public Category? Category { get; set; }

        // Event belongs to a route
        [ForeignKey(nameof(RouteId))]
        public Route? Route { get; set; }

        // Event has many enrolments
        public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
    }
}
