using System.ComponentModel.DataAnnotations;

namespace OexaDentalClinic.Api.Models
{
    public class Promotion
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Title { get; set; } = null!;

        [MaxLength(500)]
        public string? Description { get; set; }

        public int DiscountPercent { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; } = true;

        [MaxLength(100)]
        public string? TargetAudience { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
