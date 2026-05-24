using System.ComponentModel.DataAnnotations;

namespace OexaDentalClinic.Api.DTOs
{
    public class RoleDefinitionDto
    {
        [Required, MaxLength(80)]
        public string DisplayName { get; set; } = null!;

        [MaxLength(30)]
        public string? Key { get; set; }
    }
}
