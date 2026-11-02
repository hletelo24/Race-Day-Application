using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace front_end_web.Models.Profile;

public class ProfileViewModel
{
    public int UserId { get; set; }

    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [EmailAddress]
    public string EmailAddress { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    [Display(Name = "Profile picture")]
    public IFormFile? Image { get; set; }
}
