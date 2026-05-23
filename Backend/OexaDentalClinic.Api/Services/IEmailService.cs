using OexaDentalClinic.Api.Models;

namespace OexaDentalClinic.Api.Services
{
    public interface IEmailService
    {
        Task SendAppointmentBookedAsync(Appointment appointment);
        Task SendAppointmentAssignedAsync(Appointment appointment, User dentist);
        Task SendTreatmentLineAssignedAsync(Appointment appointment, AppointmentTreatment line, User dentist, string treatmentName);
        Task SendAppointmentCancelledAsync(Appointment appointment);
        Task SendAppointmentReminderAsync(Appointment appointment);
        Task SendReceiptFinalizedAsync(Appointment appointment, Receipt receipt, IEnumerable<ReceiptMedication> medications);
        Task SendStatusChangedAsync(Appointment appointment, string message);
        Task SendAppointmentRescheduledAsync(Appointment appointment, DateTime previousDateTime);
        Task SendReceiptPendingPricingAsync(Appointment appointment, Receipt receipt);
        Task SendReviewSubmittedAsync(Appointment appointment, Review review);
    }
}
