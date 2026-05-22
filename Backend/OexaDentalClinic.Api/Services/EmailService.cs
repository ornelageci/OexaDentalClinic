using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MimeKit;
using OexaDentalClinic.Api.Configuration;
using OexaDentalClinic.Api.Data;
using OexaDentalClinic.Api.Models;

namespace OexaDentalClinic.Api.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly AppDbContext _db;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> settings, AppDbContext db, ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _db = db;
            _logger = logger;
        }

        public async Task SendAppointmentBookedAsync(Appointment appointment)
        {
            var problem = await ProblemName(appointment.ServiceNeeded);
            await SendToAllPartiesAsync(appointment, null,
                "Appointment request received - Oexa Dental Clinic",
                $"A new appointment was booked.\nProblem: {problem}\nDentist will be assigned by reception.");
        }

        public Task SendAppointmentAssignedAsync(Appointment appointment, User dentist) =>
            SendToAllPartiesAsync(appointment, dentist,
                "Dentist assigned - Oexa Dental Clinic",
                $"Your appointment now has an assigned dentist: Dr. {dentist.FirstName} {dentist.LastName}.");

        public Task SendAppointmentCancelledAsync(Appointment appointment) =>
            SendToAllPartiesAsync(appointment, null,
                "Appointment cancelled - Oexa Dental Clinic",
                "The appointment has been cancelled.");

        public Task SendAppointmentReminderAsync(Appointment appointment) =>
            SendToAllPartiesAsync(appointment, null,
                "Appointment reminder - Oexa Dental Clinic",
                "Reminder: you have an upcoming dental visit.");

        public Task SendReceiptFinalizedAsync(Appointment appointment, Receipt receipt, IEnumerable<ReceiptMedication> medications)
        {
            var medLines = string.Join("\n", medications.Select(m => $"- {m.Name}: {m.UnitPrice:C}"));
            return SendToAllPartiesAsync(appointment, null,
                "Receipt finalized - Oexa Dental Clinic",
                $"Receipt {receipt.ReceiptNumber}\nTotal: {receipt.TotalAmount:C}\n\nMedications:\n{medLines}");
        }

        public Task SendStatusChangedAsync(Appointment appointment, string message) =>
            SendToAllPartiesAsync(appointment, null,
                "Appointment update - Oexa Dental Clinic",
                message);

        private async Task SendToAllPartiesAsync(Appointment appointment, User? dentist, string subject, string bodyText)
        {
            var problem = await ProblemName(appointment.ServiceNeeded);
            var dentistLine = dentist != null ? $"Dentist: Dr. {dentist.FirstName} {dentist.LastName}\n" : "Dentist: To be assigned\n";

            var text = $"""
                {bodyText}

                Patient: {appointment.FirstName} {appointment.LastName}
                Email: {appointment.Email}
                Problem: {problem}
                {dentistLine}Date & time: {appointment.PreferredDateTime:g}
                Status: {appointment.Status}

                Oexa Dental Clinic
                Tirane Delijorgji | WhatsApp: +355 69 68 67 665
                """;

            var recipients = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                appointment.Email.Trim()
            };

            var staff = await _db.Users
                .Where(u => u.Role == "Admin" || u.Role == "Manager" ||
                    (dentist != null && u.Id == dentist.Id) ||
                    (appointment.AssignedDentistUserId != null && u.Id == appointment.AssignedDentistUserId))
                .Select(u => u.Email)
                .ToListAsync();

            foreach (var email in staff)
                recipients.Add(email.Trim());

            if (dentist != null)
                recipients.Add(dentist.Email.Trim());

            await SendAsync(recipients, subject, text);
        }

        private async Task<string> ProblemName(string key)
        {
            var p = await _db.DentalProblems.FirstOrDefaultAsync(x => x.Key == key);
            return p?.Name ?? key;
        }

        private async Task SendAsync(IEnumerable<string> toAddresses, string subject, string textBody)
        {
            var list = toAddresses.Where(e => !string.IsNullOrWhiteSpace(e)).Distinct().ToList();
            if (list.Count == 0) return;

            if (!_settings.Enabled || string.IsNullOrWhiteSpace(_settings.SmtpPassword))
            {
                _logger.LogWarning("Email skipped (Enabled={Enabled}, has password={HasPwd}): {Subject}",
                    _settings.Enabled, !string.IsNullOrWhiteSpace(_settings.SmtpPassword), subject);
                return;
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
            foreach (var to in list)
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
                _logger.LogInformation("Email sent: {Subject} -> {Recipients}", subject, string.Join(", ", list));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email: {Subject} -> {Recipients}", subject, string.Join(", ", list));
            }
        }
    }
}
