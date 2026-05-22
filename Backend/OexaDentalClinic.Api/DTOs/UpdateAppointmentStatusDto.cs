using System.ComponentModel.DataAnnotations;

namespace OexaDentalClinic.Api.DTOs
{
    public class UpdateAppointmentStatusDto
    {
        [Required]
        public string Status { get; set; } = null!;
    }
}
