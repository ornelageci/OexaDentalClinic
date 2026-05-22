using OexaDentalClinic.Api.Models;

namespace OexaDentalClinic.Api.Services
{
    public interface IEmailService
    {
        Task SendAppointmentBookedAsync(Appointment appointment);
        Task SendAppointmentAssignedAsync(Appointment appointment, User dentist);
        Task SendAppointmentCancelledAsync(Appointment appointment);
        Task SendAppointmentReminderAsync(Appointment appointment);
        Task SendReceiptFinalizedAsync(Appointment appointment, Receipt receipt, IEnumerable<ReceiptMedication> medications);
        Task SendStatusChangedAsync(Appointment appointment, string message);
    }
}
