using System.ComponentModel.DataAnnotations;

namespace race_day_backend_api.Models.Auth
{
    public class RegisterRequest
    {
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string EmailAddress { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^(Participant|Organiser)$")]
        public string Role { get; set; } = "Participant";

        public string? ImageUrl { get; set; }

        public string? Gender { get; set; }

        [Range(1, 120)]
        public int? Age { get; set; }

        public string? AccessLevel { get; set; }
    }
}