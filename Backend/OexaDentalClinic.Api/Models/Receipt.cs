using System.ComponentModel.DataAnnotations;

namespace OexaDentalClinic.Api.Models
{
    public class Receipt
    {
        public int Id { get; set; }

        public int AppointmentId { get; set; }

        [Required, MaxLength(50)]
        public string ReceiptNumber { get; set; } = null!;

        public decimal TotalAmount { get; set; }

        [Required, MaxLength(30)]
        public string Status { get; set; } = "Draft";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
