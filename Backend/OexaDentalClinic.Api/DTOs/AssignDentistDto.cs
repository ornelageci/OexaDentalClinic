using System.ComponentModel.DataAnnotations;

namespace OexaDentalClinic.Api.DTOs
{
    public class AssignDentistDto
    {
        [Required]
        public int DentistUserId { get; set; }
    }
}
