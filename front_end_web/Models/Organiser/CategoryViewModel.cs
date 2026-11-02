using System.ComponentModel.DataAnnotations;

namespace front_end_web.Models.Organiser;

public class CategoryViewModel
{
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Category title is required.")]
    [StringLength(
        150,
        ErrorMessage = "The title cannot exceed 150 characters.")]
    [Display(Name = "Category Title")]
    public string Title { get; set; } = string.Empty;

    [StringLength(
        1000,
        ErrorMessage = "The description cannot exceed 1000 characters.")]
    [Display(Name = "Description")]
    public string? Description { get; set; }
}