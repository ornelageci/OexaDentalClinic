using System.ComponentModel.DataAnnotations;

namespace OexaDentalClinic.Api.DTOs
{
    public class RescheduleTreatmentDto
    {
        [Required]
        public string ProblemKey { get; set; } = null!;

        [Required]
        public string PreferredDate { get; set; } = null!;

        [Required]
        public string PreferredTime { get; set; } = null!;

        public int? DentistUserId { get; set; }
    }
}
