using System.ComponentModel.DataAnnotations;

namespace OexaDentalClinic.Api.Models
{
    public class Receipt
    {
        public int Id { get; set; }

        public int AppointmentId { get; set; }

        [Required, MaxLength(50)]
        public string ReceiptNumber { get; set; } = null!;

        /// <summary>Sum of treatments + medications before TVSH.</summary>
        public decimal SubtotalBeforeVat { get; set; }

        /// <summary>TVSH (VAT) — 20% of subtotal.</summary>
        public decimal VatAmount { get; set; }

        /// <summary>Total after TVSH (subtotal + VAT).</summary>
        public decimal TotalAmount { get; set; }

        [Required, MaxLength(30)]
        public string Status { get; set; } = "Draft";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
