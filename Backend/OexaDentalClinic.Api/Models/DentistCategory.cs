using System.ComponentModel.DataAnnotations;

namespace OexaDentalClinic.Api.Models
{
    public class DentistCategory
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Key { get; set; } = null!;

        [Required, MaxLength(100)]
        public string DisplayName { get; set; } = null!;

        public int SortOrder { get; set; }
    }
}
