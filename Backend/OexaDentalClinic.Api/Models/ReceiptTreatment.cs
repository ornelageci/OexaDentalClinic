using System.ComponentModel.DataAnnotations;

namespace OexaDentalClinic.Api.Models
{
    public class ReceiptTreatment
    {
        public int Id { get; set; }

        public int ReceiptId { get; set; }

        public int? AppointmentTreatmentId { get; set; }

        [Required, MaxLength(80)]
        public string ProblemKey { get; set; } = null!;

        [Required, MaxLength(200)]
        public string Name { get; set; } = null!;

        public int? DentistUserId { get; set; }

        public decimal? UnitPrice { get; set; }
    }
}
