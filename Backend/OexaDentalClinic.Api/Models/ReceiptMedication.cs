using System.ComponentModel.DataAnnotations;

namespace OexaDentalClinic.Api.Models
{
    public class ReceiptMedication
    {
        public int Id { get; set; }

        public int ReceiptId { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = null!;

        public decimal? UnitPrice { get; set; }
    }
}
