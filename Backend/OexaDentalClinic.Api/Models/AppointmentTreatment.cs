using System.ComponentModel.DataAnnotations;

namespace OexaDentalClinic.Api.Models
{
    /// <summary>One treatment line within a booking — can have its own dentist and time.</summary>
    public class AppointmentTreatment
    {
        public int Id { get; set; }

        public int AppointmentId { get; set; }

        [Required, MaxLength(80)]
        public string ProblemKey { get; set; } = null!;

        public int? AssignedDentistUserId { get; set; }

        public DateTime ScheduledStart { get; set; }

        public int DurationMinutes { get; set; } = 60;
    }
}
