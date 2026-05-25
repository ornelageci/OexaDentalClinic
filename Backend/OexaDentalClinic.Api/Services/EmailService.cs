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
            var problem = await FormatServiceNames(appointment.ServiceNeeded);
            var schedule = await FormatProposedScheduleAsync(appointment.Id);
            var scheduleLine = string.IsNullOrWhiteSpace(schedule)
                ? ""
                : "\nProposed schedule (shorter treatment first):\n" + schedule + "\n";
            await SendToAllPartiesAsync(appointment, null,
                "Appointment request received - Oexa Dental Clinic",
                $"A new appointment was booked.\nTreatments: {problem}{scheduleLine}\nDentist will be assigned by reception.");
        }

        public async Task SendAppointmentAssignedAsync(Appointment appointment, User dentist)
        {
            var (appt, resolved) = await ResolveAppointmentContextAsync(appointment, dentist);
            var d = resolved ?? dentist;
            var lines = await GetAssignedTreatmentLinesAsync(appt.Id);
            var distinctDentists = lines.Select(l => l.DentistId).Distinct().Count();

            string body;
            if (distinctDentists > 1)
            {
                var schedule = await FormatTreatmentScheduleAsync(appt.Id);
                body = "All treatments for your visit are now assigned:\n" + schedule;
                await SendToAllPartiesAsync(appt, null,
                    "Dentists assigned - Oexa Dental Clinic", body);
                return;
            }

            body = d != null
                ? $"Your appointment now has an assigned dentist: Dr. {d.FirstName} {d.LastName}."
                : "Your appointment dentists have been assigned.";
            await SendToAllPartiesAsync(appt, d,
                "Dentist assigned - Oexa Dental Clinic", body);
        }

        public async Task SendTreatmentLineAssignedAsync(Appointment appointment, AppointmentTreatment line, User dentist, string treatmentName)
        {
            var (appt, _) = await ResolveAppointmentContextAsync(appointment, dentist);
            var end = line.ScheduledStart.AddMinutes(line.DurationMinutes > 0 ? line.DurationMinutes : 60);
            await SendToAllPartiesAsync(appt, dentist,
                "Treatment assigned - Oexa Dental Clinic",
                $"Dr. {dentist.FirstName} {dentist.LastName} was assigned for {treatmentName} at {line.ScheduledStart:HH:mm} – {end:HH:mm}. Other treatments may still need assignment.");
        }

        public async Task SendTreatmentLineRescheduledAsync(Appointment appointment, AppointmentTreatment line, User dentist, string treatmentName, DateTime previousStart)
        {
            var (appt, _) = await ResolveAppointmentContextAsync(appointment, dentist);
            var end = line.ScheduledStart.AddMinutes(line.DurationMinutes > 0 ? line.DurationMinutes : 60);
            await SendToAllPartiesAsync(appt, dentist,
                "Treatment rescheduled - Oexa Dental Clinic",
                $"{treatmentName} with Dr. {dentist.FirstName} {dentist.LastName} was rescheduled.\nPrevious: {previousStart:g}\nNew: {line.ScheduledStart:HH:mm} – {end:HH:mm}");
        }

        public async Task SendAppointmentCancelledAsync(Appointment appointment)
        {
            var (appt, dentist) = await ResolveAppointmentContextAsync(appointment, null);
            await SendToAllPartiesAsync(appt, dentist,
                "Appointment cancelled - Oexa Dental Clinic",
                AppendDentistToMessage("The appointment has been cancelled.", dentist));
        }

        public async Task SendAppointmentReminderAsync(Appointment appointment)
        {
            var (appt, dentist) = await ResolveAppointmentContextAsync(appointment, null);
            await SendToAllPartiesAsync(appt, dentist,
                "Appointment reminder - Oexa Dental Clinic",
                AppendDentistToMessage("Reminder: you have an upcoming dental visit.", dentist));
        }

        public async Task SendReceiptFinalizedAsync(
            Appointment appointment,
            Receipt receipt,
            IEnumerable<ReceiptMedication> medications,
            IEnumerable<ReceiptTreatment> treatments)
        {
            var (appt, dentist) = await ResolveAppointmentContextAsync(appointment, null);
            var treatmentLines = string.Join("\n", treatments
                .Where(t => t.UnitPrice.HasValue)
                .Select(t => $"- {t.Name}: {t.UnitPrice:0.00} EUR"));
            var medLines = string.Join("\n", medications
                .Where(m => m.UnitPrice.HasValue)
                .Select(m => $"- {m.Name}: {m.UnitPrice:0.00} EUR"));
            var subtotal = receipt.SubtotalBeforeVat > 0
                ? receipt.SubtotalBeforeVat
                : VatHelper.FromTotalIncludingVat(receipt.TotalAmount).Subtotal;
            var vat = receipt.VatAmount > 0
                ? receipt.VatAmount
                : VatHelper.FromTotalIncludingVat(receipt.TotalAmount).Vat;

            var body = $"Receipt {receipt.ReceiptNumber}\n" +
                       $"Subtotal (before TVSH): {subtotal:0.00} EUR\n" +
                       $"TVSH ({VatHelper.RatePercent}%): {vat:0.00} EUR\n" +
                       $"Total (after TVSH): {receipt.TotalAmount:0.00} EUR";
            if (!string.IsNullOrWhiteSpace(treatmentLines))
                body += "\n\nTreatments:\n" + treatmentLines;
            if (!string.IsNullOrWhiteSpace(medLines))
                body += "\n\nMedications:\n" + medLines;
            await SendToAllPartiesAsync(appt, dentist,
                "Receipt finalized - Oexa Dental Clinic",
                AppendDentistToMessage(body, dentist));
        }

        public async Task SendStatusChangedAsync(Appointment appointment, string message)
        {
            var (appt, dentist) = await ResolveAppointmentContextAsync(appointment, null);
            await SendToAllPartiesAsync(appt, dentist,
                "Appointment update - Oexa Dental Clinic",
                AppendDentistToMessage(message, dentist));
        }

        public async Task SendAppointmentRescheduledAsync(Appointment appointment, DateTime previousDateTime)
        {
            var (appt, dentist) = await ResolveAppointmentContextAsync(appointment, null);
            var schedule = await FormatProposedScheduleAsync(appt.Id);
            var scheduleText = string.IsNullOrWhiteSpace(schedule)
                ? $"New visit start: {appt.PreferredDateTime:g}"
                : "New schedule:\n" + schedule;
            await SendToAllPartiesAsync(appt, dentist,
                "Appointment rescheduled - Oexa Dental Clinic",
                AppendDentistToMessage(
                    $"Your appointment was rescheduled.\nPrevious visit start: {previousDateTime:g}\n{scheduleText}",
                    dentist));
        }

        public async Task SendReceiptPendingPricingAsync(Appointment appointment, Receipt receipt)
        {
            var (appt, dentist) = await ResolveAppointmentContextAsync(appointment, null);
            await SendToAllPartiesAsync(appt, dentist,
                "Receipt ready for pricing - Oexa Dental Clinic",
                AppendDentistToMessage(
                    $"Dentist submitted medications for appointment #{appt.Id}. Receipt {receipt.ReceiptNumber} is waiting for manager pricing.",
                    dentist));
        }

        public async Task SendReviewSubmittedAsync(Appointment appointment, Review review)
        {
            var (appt, dentist) = await ResolveAppointmentContextAsync(appointment, null);
            var treatments = await FormatServiceNames(appt.ServiceNeeded);
            var dentistName = dentist != null ? $"Dr. {dentist.FirstName} {dentist.LastName}" : "Not assigned";
            var comment = string.IsNullOrWhiteSpace(review.Comment) ? "(no comment)" : review.Comment.Trim();

            var body = $"""
                A patient submitted a new review.

                Rating: {review.Rating}/5
                Message:
                {comment}

                Appointment #{appt.Id}
                Patient: {appt.FirstName} {appt.LastName}
                Email: {appt.Email}
                Phone: {appt.PhoneNumber}
                Date & time: {appt.PreferredDateTime:g}
                Treatments: {treatments}
                Dentist: {dentistName}

                Oexa Dental Clinic
                """;

            var clinicEmail = string.IsNullOrWhiteSpace(_settings.ClinicNotificationEmail)
                ? _settings.FromAddress
                : _settings.ClinicNotificationEmail.Trim();

            await SendAsync(new[] { clinicEmail }, "New patient review - Oexa Dental Clinic", body);
        }

        private async Task SendToAllPartiesAsync(Appointment appointment, User? dentist, string subject, string bodyText)
        {
            var (appt, resolvedDentist) = await ResolveAppointmentContextAsync(appointment, dentist);
            dentist = resolvedDentist;

            var problem = await FormatServiceNames(appt.ServiceNeeded);
            var dentistLine = dentist != null
                ? FormatDentistLine(dentist)
                : await FormatDentistLineFromLinesAsync(appt.Id);

            var scheduleBlock = await FormatScheduleBlockForEmailAsync(appt.Id);
            var timeLine = string.IsNullOrWhiteSpace(scheduleBlock)
                ? $"Date & time: {appt.PreferredDateTime:g}\n"
                : scheduleBlock;

            var text = $"""
                {bodyText}

                Patient: {appt.FirstName} {appt.LastName}
                Email: {appt.Email}
                Treatments: {problem}
                {dentistLine}{timeLine}Status: {appt.Status}

                Oexa Dental Clinic
                Tirane Delijorgji | WhatsApp: +355 69 68 67 665
                """;

            var recipients = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                appt.Email.Trim()
            };

            var lineDentistIds = await _db.AppointmentTreatments
                .Where(t => t.AppointmentId == appt.Id && t.AssignedDentistUserId != null)
                .Select(t => t.AssignedDentistUserId!.Value)
                .Distinct()
                .ToListAsync();

            var staff = await _db.Users
                .Where(u => u.Role == "Admin" || u.Role == "Manager" ||
                    (dentist != null && u.Id == dentist.Id) ||
                    (appt.AssignedDentistUserId != null && u.Id == appt.AssignedDentistUserId) ||
                    lineDentistIds.Contains(u.Id))
                .Select(u => u.Email)
                .ToListAsync();

            foreach (var email in staff)
                recipients.Add(email.Trim());

            if (dentist != null)
                recipients.Add(dentist.Email.Trim());

            await SendAsync(recipients, subject, text);
        }

        /// <summary>Reload appointment and resolve assigned dentist from the database.</summary>
        private async Task<(Appointment appt, User? dentist)> ResolveAppointmentContextAsync(Appointment appointment, User? dentist)
        {
            var appt = await _db.Appointments.AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == appointment.Id) ?? appointment;

            if (dentist == null && appt.AssignedDentistUserId.HasValue)
            {
                dentist = await _db.Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == appt.AssignedDentistUserId.Value && u.Role == "Dentist");
            }

            if (dentist == null)
            {
                var lines = await GetAssignedTreatmentLinesAsync(appt.Id);
                if (lines.Count == 1)
                {
                    dentist = await _db.Users.AsNoTracking()
                        .FirstOrDefaultAsync(u => u.Id == lines[0].DentistId && u.Role == "Dentist");
                }
            }

            return (appt, dentist);
        }

        private async Task<List<AssignedTreatmentLine>> GetAssignedTreatmentLinesAsync(int appointmentId)
        {
            return await _db.AppointmentTreatments.AsNoTracking()
                .Where(t => t.AppointmentId == appointmentId && t.AssignedDentistUserId != null)
                .Select(t => new AssignedTreatmentLine
                {
                    DentistId = t.AssignedDentistUserId!.Value,
                    ProblemKey = t.ProblemKey,
                    ScheduledStart = t.ScheduledStart
                })
                .ToListAsync();
        }

        private sealed class AssignedTreatmentLine
        {
            public int DentistId { get; set; }
            public string ProblemKey { get; set; } = "";
            public DateTime ScheduledStart { get; set; }
        }

        private async Task<string> FormatDentistLineFromLinesAsync(int appointmentId)
        {
            var lines = await GetAssignedTreatmentLinesAsync(appointmentId);
            if (lines.Count == 0)
                return "Dentist: To be assigned\n";

            if (lines.Select(l => l.DentistId).Distinct().Count() == 1)
            {
                var d = await _db.Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == lines[0].DentistId);
                return FormatDentistLine(d);
            }

            return "Dentists:\n" + await FormatTreatmentScheduleAsync(appointmentId) + "\n";
        }

        private async Task<string> FormatProposedScheduleAsync(int appointmentId)
        {
            var lines = await _db.AppointmentTreatments.AsNoTracking()
                .Where(t => t.AppointmentId == appointmentId)
                .ToListAsync();
            if (lines.Count == 0) return "";

            var names = await DentalProblemLookup.NameByKeyAsync(_db);
            var users = await _db.Users.AsNoTracking().Where(u => u.Role == "Dentist").ToDictionaryAsync(u => u.Id);

            return string.Join("\n", AppointmentSchedulingService.OrderLinesShorterFirst(lines).Select(l =>
            {
                var treatment = names.GetValueOrDefault(l.ProblemKey, l.ProblemKey);
                var end = l.ScheduledStart.AddMinutes(l.DurationMinutes > 0 ? l.DurationMinutes : 60);
                var dr = l.AssignedDentistUserId.HasValue && users.TryGetValue(l.AssignedDentistUserId.Value, out var d)
                    ? $"Dr. {d.FirstName} {d.LastName}"
                    : "Dentist TBD";
                return $"- {treatment}: {l.ScheduledStart:HH:mm} – {end:HH:mm} ({dr})";
            }));
        }

        private async Task<string> FormatScheduleBlockForEmailAsync(int appointmentId)
        {
            var lines = await _db.AppointmentTreatments.AsNoTracking()
                .Where(t => t.AppointmentId == appointmentId)
                .ToListAsync();
            if (lines.Count <= 1)
                return lines.Count == 1
                    ? $"Date & time: {lines[0].ScheduledStart:g}\n"
                    : "";

            var schedule = await FormatProposedScheduleAsync(appointmentId);
            return string.IsNullOrWhiteSpace(schedule) ? "" : "Schedule:\n" + schedule + "\n";
        }

        private async Task<string> FormatTreatmentScheduleAsync(int appointmentId)
        {
            var lines = await _db.AppointmentTreatments.AsNoTracking()
                .Where(t => t.AppointmentId == appointmentId && t.AssignedDentistUserId != null)
                .ToListAsync();
            var users = await _db.Users.AsNoTracking().Where(u => u.Role == "Dentist").ToDictionaryAsync(u => u.Id);
            var names = await DentalProblemLookup.NameByKeyAsync(_db);

            return string.Join("\n", AppointmentSchedulingService.OrderLinesShorterFirst(lines).Select(l =>
            {
                users.TryGetValue(l.AssignedDentistUserId!.Value, out var d);
                var treatment = names.GetValueOrDefault(l.ProblemKey, l.ProblemKey);
                var dr = d != null ? $"Dr. {d.FirstName} {d.LastName}" : "Dentist";
                var end = l.ScheduledStart.AddMinutes(l.DurationMinutes > 0 ? l.DurationMinutes : 60);
                return $"- {treatment}: {dr} at {l.ScheduledStart:HH:mm} – {end:HH:mm}";
            }));
        }

        private static string FormatDentistLine(User? dentist) =>
            dentist != null
                ? $"Dentist: Dr. {dentist.FirstName} {dentist.LastName}\n"
                : "Dentist: To be assigned\n";

        private static string AppendDentistToMessage(string message, User? dentist)
        {
            if (dentist == null) return message;
            var name = $"Dr. {dentist.FirstName} {dentist.LastName}";
            if (message.Contains(name, StringComparison.OrdinalIgnoreCase))
                return message;
            return message + $"\n\nYour dentist: {name}.";
        }

        private async Task<string> FormatServiceNames(string serviceNeeded)
        {
            var keys = AppointmentSchedulingService.ParseServiceKeys(serviceNeeded);
            if (keys.Count == 0) return serviceNeeded;

            var nameByKey = (await _db.DentalProblems.AsNoTracking().ToListAsync())
                .ToDictionary(p => p.Key, p => p.Name, StringComparer.OrdinalIgnoreCase);

            return string.Join(", ", keys.Select(k => nameByKey.GetValueOrDefault(k, k)));
        }

        private async Task SendAsync(IEnumerable<string> toAddresses, string subject, string textBody)
        {
            var list = toAddresses.Where(e => !string.IsNullOrWhiteSpace(e)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (list.Count == 0) return;

            if (!_settings.Enabled || string.IsNullOrWhiteSpace(_settings.SmtpPassword))
            {
                _logger.LogWarning("Email skipped (Enabled={Enabled}, has password={HasPwd}): {Subject}",
                    _settings.Enabled, !string.IsNullOrWhiteSpace(_settings.SmtpPassword), subject);
                return;
            }

            var sent = 0;
            foreach (var to in list)
            {
                if (await TrySendOneAsync(to, subject, textBody))
                    sent++;
            }

            _logger.LogInformation("Email {Subject}: sent {Sent}/{Total}", subject, sent, list.Count);
        }

        private async Task<bool> TrySendOneAsync(string to, string subject, string textBody)
        {
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
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email: {Subject} -> {Recipient}", subject, to);
                return false;
            }
        }
    }
}
