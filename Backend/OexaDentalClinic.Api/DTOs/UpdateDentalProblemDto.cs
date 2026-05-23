using System.ComponentModel.DataAnnotations;

namespace OexaDentalClinic.Api.DTOs
{
    public class UpdateDentalProblemDto
    {
        [Required, MaxLength(150)]
        public string Name { get; set; } = null!;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Range(0.01, 100000)]
        public decimal BasePrice { get; set; }

        [Range(15, 480)]
        public int DurationMinutes { get; set; }

        [Required, MaxLength(50)]
        public string DentistCategoryKey { get; set; } = null!;
    }
}
