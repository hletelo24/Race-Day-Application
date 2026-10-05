using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace race_day_backend_api.Models
{
    public class Organiser
    {
        [Key]
        [ForeignKey(nameof(UserAccount))]
        public int UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string AccessLevel { get; set; } = string.Empty;

        // Navigation
        public UserAccount UserAccount { get; set; } = null!;

        public ICollection<Event> Events { get; set; } = new List<Event>();
    }
}
