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

    public class PriceReceiptTreatmentLineDto
    {
        [Required]
        public int TreatmentLineId { get; set; }

        [Required]
        public decimal UnitPrice { get; set; }
    }

    public class PriceReceiptDto
    {
        public List<PriceReceiptLineDto> MedicationLines { get; set; } = new();

        public List<PriceReceiptTreatmentLineDto> TreatmentLines { get; set; } = new();
    }
}
