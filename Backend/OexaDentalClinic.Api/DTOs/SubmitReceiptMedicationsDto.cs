using System.ComponentModel.DataAnnotations;

namespace OexaDentalClinic.Api.DTOs
{
    public class SubmitReceiptMedicationsDto
    {
        [Required]
        public int AppointmentId { get; set; }

        [Required, MinLength(1)]
        public List<string> Medications { get; set; } = new();
    }
}
