using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace race_day_backend_api.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        // Navigation
        [JsonIgnore]
        public ICollection<Event> Events { get; set; } = new List<Event>();
    }
}
