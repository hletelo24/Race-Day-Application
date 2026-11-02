using System.ComponentModel.DataAnnotations;

namespace race_day_backend_api.DTOs
{
    public class EnrolmentRequestDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int EventId { get; set; }
    }
}