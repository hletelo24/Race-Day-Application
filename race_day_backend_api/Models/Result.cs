using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace race_day_backend_api.Models
{
    public class Result
    {
        [Key]
        public int ResultsId { get; set; }

        [Required]
        public int EnrolmentId { get; set; }

        public TimeSpan Time { get; set; }

        // Navigation
        [ForeignKey(nameof(EnrolmentId))]
        public Enrolment Enrolment { get; set; } = null!;
    }
}
