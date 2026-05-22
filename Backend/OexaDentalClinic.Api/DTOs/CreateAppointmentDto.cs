using System.ComponentModel.DataAnnotations; // Provides validation attributes

namespace OexaDentalClinic.Api.DTOs
{
    // Data Transfer Object used when creating an appointment
    // This class defines the shape of data sent from the frontend
    public class CreateAppointmentDto
    {
        // Patient first name (required)
        [Required]
        public string FirstName { get; set; } = null!;

        // Patient last name (required)
        [Required]
        public string LastName { get; set; } = null!;

        // Patient email address (required and must be a valid email format)
        [Required, EmailAddress]
        public string Email { get; set; } = null!;

        // Contact phone number (required)
        [Required]
        public string PhoneNumber { get; set; } = null!;

        // Frontend sends date like "dd.mm.yyyy" or "yyyy-mm-dd"
        // Parsed and validated later in the controller
        [Required]
        public string PreferredDate { get; set; } = null!;

        // Frontend sends time like "09:00" or "10:30"
        // Parsed and validated later in the controller
        [Required]
        public string PreferredTime { get; set; } = null!;

        // Requested dental service (required)
        [Required]
        public string ServiceNeeded { get; set; } = null!;

        public string? AdditionalNotes { get; set; }

        public bool IsSpecialAppointment { get; set; }

        public int? PatientUserId { get; set; }
    }
}

// A DTO is used to transfer data between client and server without exposing the database entity.
