using System.ComponentModel.DataAnnotations;

namespace OexaDentalClinic.Api.DTOs
{
    public class AssignTreatmentDto
    {
        [Required]
        public string ProblemKey { get; set; } = null!;

        [Required]
        public int DentistUserId { get; set; }

        /// <summary>Optional — reschedule only this treatment (dd.MM.yyyy or yyyy-MM-dd).</summary>
        public string? PreferredDate { get; set; }

        public string? PreferredTime { get; set; }
    }
}
