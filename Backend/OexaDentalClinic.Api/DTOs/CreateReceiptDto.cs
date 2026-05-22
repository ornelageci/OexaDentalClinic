using System.ComponentModel.DataAnnotations;

namespace OexaDentalClinic.Api.DTOs
{
    public class CreateReceiptDto
    {
        [Required]
        public int AppointmentId { get; set; }

        public decimal TotalAmount { get; set; }
    }
}
