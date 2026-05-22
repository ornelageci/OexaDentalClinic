using System.ComponentModel.DataAnnotations;

namespace OexaDentalClinic.Api.DTOs
{
    public class PriceReceiptLineDto
    {
        [Required]
        public int MedicationId { get; set; }

        [Required]
        public decimal UnitPrice { get; set; }
    }

    public class PriceReceiptDto
    {
        [Required, MinLength(1)]
        public List<PriceReceiptLineDto> Lines { get; set; } = new();
    }
}
