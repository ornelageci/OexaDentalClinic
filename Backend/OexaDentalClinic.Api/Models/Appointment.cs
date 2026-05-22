using System.ComponentModel.DataAnnotations; // Provides validation and schema attributes

namespace OexaDentalClinic.Api.Models
{
    // Represents the Appointment entity stored in the database
    public class Appointment
    {
        // Primary key (auto-incremented by the database)
        public int Id { get; set; }

        // Patient first name (required, max 100 characters)
        [Required, MaxLength(100)]
        public string FirstName { get; set; } = null!;

        // Patient last name (required, max 100 characters)
        [Required, MaxLength(100)]
        public string LastName { get; set; } = null!;

        // Patient email address (required, valid email, max 150 characters)
        [Required, EmailAddress, MaxLength(150)]
        public string Email { get; set; } = null!;

        // Contact phone number (required, max 50 characters)
        [Required, MaxLength(50)]
        public string PhoneNumber { get; set; } = null!;

        // Combined date + time (best practice for database storage and sorting)
        [Required]
        public DateTime PreferredDateTime { get; set; }

        // Requested dental service (required, max 150 characters)
        [Required, MaxLength(150)]
        public string ServiceNeeded { get; set; } = null!;

        public string? AdditionalNotes { get; set; }

        [Required, MaxLength(30)]
        public string Status { get; set; } = "Booked";

        public bool IsSpecialAppointment { get; set; }

        public int? PatientUserId { get; set; }

        public int? AssignedDentistUserId { get; set; }

        public bool ReminderSent { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}


// An entity represents a table in the database and defines how data is stored.