using System.ComponentModel.DataAnnotations;

namespace OexaDentalClinic.Api.DTOs
{
    public class CompleteVisitDto
    {
        [Required]
        public int DentistUserId { get; set; }
    }
}
