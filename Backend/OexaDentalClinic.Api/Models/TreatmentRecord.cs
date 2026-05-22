using System.ComponentModel.DataAnnotations;

namespace OexaDentalClinic.Api.Models
{
    public class TreatmentRecord
    {
        public int Id { get; set; }

        public int AppointmentId { get; set; }

        [MaxLength(500)]
        public string? Diagnosis { get; set; }

        [MaxLength(500)]
        public string? TreatmentPerformed { get; set; }

        [MaxLength(500)]
        public string? Recommendations { get; set; }

        [MaxLength(300)]
        public string? MedicationPrescribed { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
