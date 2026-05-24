using System.ComponentModel.DataAnnotations;

namespace OexaDentalClinic.Api.Models
{
    public class UserRoleDefinition
    {
        public int Id { get; set; }

        [Required, MaxLength(30)]
        public string Key { get; set; } = null!;

        [Required, MaxLength(80)]
        public string DisplayName { get; set; } = null!;

        public int SortOrder { get; set; }

        public bool IsSystem { get; set; }
    }
}
