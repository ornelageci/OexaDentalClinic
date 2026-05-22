using OexaDentalClinic.Api.Models;

namespace OexaDentalClinic.Api.Services
{
    public interface IEmailService
    {
        Task SendAppointmentConfirmationAsync(Appointment appointment);
        Task SendAppointmentCancelledAsync(Appointment appointment);
        Task SendAppointmentReminderAsync(Appointment appointment);
    }
}
