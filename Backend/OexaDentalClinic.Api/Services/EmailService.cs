using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using OexaDentalClinic.Api.Configuration;
using OexaDentalClinic.Api.Models;

namespace OexaDentalClinic.Api.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public Task SendAppointmentConfirmationAsync(Appointment appointment) =>
            SendAsync(
                appointment.Email,
                "Appointment confirmed - Oexa Dental Clinic",
                Body(
                    $"Hello {appointment.FirstName},",
                    "Your appointment is confirmed.",
                    appointment));

        public Task SendAppointmentCancelledAsync(Appointment appointment) =>
            SendAsync(
                appointment.Email,
                "Appointment cancelled - Oexa Dental Clinic",
                Body(
                    $"Hello {appointment.FirstName},",
                    "Your appointment has been cancelled.",
                    appointment));

        public Task SendAppointmentReminderAsync(Appointment appointment) =>
            SendAsync(
                appointment.Email,
                "Appointment reminder - Oexa Dental Clinic",
                Body(
                    $"Hello {appointment.FirstName},",
                    "This is a reminder for your upcoming visit.",
                    appointment));

        private static string Body(string greeting, string message, Appointment appointment)
        {
            return $"""
                {greeting}

                {message}

                Date & time: {appointment.PreferredDateTime:g}
                Service: {appointment.ServiceNeeded}
                Status: {appointment.Status}

                Oexa Dental Clinic
                Tirane Delijorgji
                WhatsApp: +355 69 68 67 665
                """;
        }

        private async Task SendAsync(string to, string subject, string textBody)
        {
            if (!_settings.Enabled || string.IsNullOrWhiteSpace(_settings.SmtpPassword))
            {
                _logger.LogInformation("Email skipped (disabled or no SMTP password): {Subject} -> {To}", subject, to);
                return;
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = textBody };

            try
            {
                using var client = new SmtpClient();
                await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_settings.SmtpUser, _settings.SmtpPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
                _logger.LogInformation("Email sent: {Subject} -> {To}", subject, to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email: {Subject} -> {To}", subject, to);
            }
        }
    }
}
