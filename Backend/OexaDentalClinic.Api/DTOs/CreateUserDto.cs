using System.ComponentModel.DataAnnotations;

namespace OexaDentalClinic.Api.DTOs
{
    public class CreateUserDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = null!;

        [Required, MinLength(4)]
        public string Password { get; set; } = null!;

        [Required]
        public string FirstName { get; set; } = null!;

        [Required]
        public string LastName { get; set; } = null!;

        [Required]
        public string Role { get; set; } = null!;

        public string? PhoneNumber { get; set; }

        public string? DentistServiceKey { get; set; }
    }
}
