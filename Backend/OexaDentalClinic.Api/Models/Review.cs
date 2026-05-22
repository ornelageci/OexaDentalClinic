using System.ComponentModel.DataAnnotations;

namespace OexaDentalClinic.Api.Models
{
    public class Review
    {
        public int Id { get; set; }

        public int AppointmentId { get; set; }

        public int Rating { get; set; }

        [MaxLength(500)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
