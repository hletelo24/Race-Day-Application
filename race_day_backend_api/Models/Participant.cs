using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace race_day_backend_api.Models
{
    public class Participant
    {
        [Key]
        [ForeignKey(nameof(UserAccount))]
        public int UserId { get; set; }

        [Required]
        [MaxLength(20)]
        public string Gender { get; set; } = string.Empty;

        public int Age { get; set; }

        [MaxLength(50)]
        public string Role { get; set; } = "Participant";

        // Navigation
        public UserAccount UserAccount { get; set; } = null!;

        public ICollection<Enrolment> Enrolments { get; set; }
            = new List<Enrolment>();
    }
}
