using System.ComponentModel.DataAnnotations;

namespace OexaDentalClinic.Api.DTOs
{
    public class CreatePromotionDto
    {
        [Required]
        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        public int DiscountPercent { get; set; }

        [Required]
        public string StartDate { get; set; } = null!;

        [Required]
        public string EndDate { get; set; } = null!;

        public string? TargetAudience { get; set; }

        public string? ProblemKey { get; set; }
    }
}
