using System.ComponentModel.DataAnnotations;

namespace front_end_web.Models.Auth;

public class RegisterViewModel
{
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string EmailAddress { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 8)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(Participant|Organiser)$",
        ErrorMessage = "Select Participant or Organiser.")]
    public string Role { get; set; } = "Participant";

    public string? Gender { get; set; }

    [Range(1, 120, ErrorMessage = "Enter a valid age.")]
    public int? Age { get; set; }

    public string? AccessLevel { get; set; }
}