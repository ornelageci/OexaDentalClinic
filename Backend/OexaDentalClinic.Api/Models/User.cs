using System.ComponentModel.DataAnnotations;

namespace OexaDentalClinic.Api.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Email { get; set; } = null!;

        [Required, MaxLength(100)]
        public string Password { get; set; } = null!;

        [Required, MaxLength(100)]
        public string FirstName { get; set; } = null!;

        [Required, MaxLength(100)]
        public string LastName { get; set; } = null!;

        [MaxLength(50)]
        public string? PhoneNumber { get; set; }

        [Required, MaxLength(30)]
        public string Role { get; set; } = "Patient";

        [MaxLength(50)]
        public string? DentistServiceKey { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
