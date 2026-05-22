using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OexaDentalClinic.Api.Configuration;
using OexaDentalClinic.Api.Data;

namespace OexaDentalClinic.Api.Services
{
    public class AppointmentReminderService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly EmailSettings _settings;
        private readonly ILogger<AppointmentReminderService> _logger;

        public AppointmentReminderService(
            IServiceProvider services,
            IOptions<EmailSettings> settings,
            ILogger<AppointmentReminderService> logger)
        {
            _services = services;
            _settings = settings.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SendDueRemindersAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Reminder job failed.");
                }

                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private async Task SendDueRemindersAsync(CancellationToken cancellationToken)
        {
            if (!_settings.Enabled) return;

            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var email = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var now = DateTime.Now;
            var windowStart = now.AddHours(_settings.ReminderHoursBefore);
            var windowEnd = windowStart.AddHours(1);

            var due = await db.Appointments
                .Where(a => a.Status == "Booked" && !a.ReminderSent
                    && a.PreferredDateTime >= windowStart
                    && a.PreferredDateTime < windowEnd)
                .ToListAsync(cancellationToken);

            foreach (var appt in due)
            {
                await email.SendAppointmentReminderAsync(appt);
                appt.ReminderSent = true;
            }

            if (due.Count > 0)
                await db.SaveChangesAsync(cancellationToken);
        }
    }
}
