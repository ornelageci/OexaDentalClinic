namespace OexaDentalClinic.Api.DTOs
{
    public class UpdateTreatmentDto
    {
        public string? Diagnosis { get; set; }
        public string? TreatmentPerformed { get; set; }
        public string? Recommendations { get; set; }
        public string? MedicationPrescribed { get; set; }
    }
}
