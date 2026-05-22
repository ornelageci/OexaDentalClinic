using System.ComponentModel.DataAnnotations;

namespace OexaDentalClinic.Api.Models
{
    public class DentalProblem
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Key { get; set; } = null!;

        [Required, MaxLength(150)]
        public string Name { get; set; } = null!;

        [MaxLength(500)]
        public string? Description { get; set; }

        public decimal BasePrice { get; set; }

        [Required, MaxLength(50)]
        public string DentistCategoryKey { get; set; } = null!;
    }
}
