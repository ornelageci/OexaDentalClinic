using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OexaDentalClinic.Api.Data;
using OexaDentalClinic.Api.DTOs;
using OexaDentalClinic.Api.Models;
using OexaDentalClinic.Api.Services;
using System.Globalization;

namespace OexaDentalClinic.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AppointmentsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IEmailService _email;
        private readonly AppointmentSchedulingService _scheduling;

        public AppointmentsController(AppDbContext db, IEmailService email, AppointmentSchedulingService scheduling)
        {
            _db = db;
            _email = email;
            _scheduling = scheduling;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAppointmentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var preferredDateTime = ParseDateTime(dto.PreferredDate, dto.PreferredTime);
            if (preferredDateTime == null)
                return BadRequest(new { error = "Invalid date or time format." });

            if (preferredDateTime.Value < DateTime.Now)
                return BadRequest(new { error = "Cannot book appointments in the past." });

            var serviceKeys = AppointmentSchedulingService.ParseServiceKeys(dto.ServiceNeeded.Trim());
            if (serviceKeys.Count == 0)
                return BadRequest(new { error = "Select at least one treatment." });

            try
            {
                await _scheduling.GetProblemsForKeysAsync(serviceKeys);
            }
            catch
            {
                return BadRequest(new { error = "Invalid treatment selected." });
            }

            if (!await _scheduling.IsSlotAvailableAsync(serviceKeys, preferredDateTime.Value))
                return BadRequest(new { error = "Selected time slot is not available." });

            var appointment = new Appointment
            {
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                Email = dto.Email.Trim(),
                PhoneNumber = dto.PhoneNumber.Trim(),
                PreferredDateTime = preferredDateTime.Value,
                ServiceNeeded = string.Join(",", serviceKeys),
                AdditionalNotes = string.IsNullOrWhiteSpace(dto.AdditionalNotes) ? null : dto.AdditionalNotes.Trim(),
                IsSpecialAppointment = dto.IsSpecialAppointment,
                PatientUserId = dto.PatientUserId,
                AssignedDentistUserId = null,
                Status = "Booked"
            };

            _db.Appointments.Add(appointment);
            await _db.SaveChangesAsync();

            await _email.SendAppointmentBookedAsync(appointment);

            return CreatedAtAction(nameof(GetById), new { id = appointment.Id }, appointment);
        }

        [HttpGet("unassigned")]
        public async Task<IActionResult> GetUnassigned()
        {
            var appts = await _db.Appointments
                .Where(a => a.AssignedDentistUserId == null && a.Status == "Booked")
                .OrderBy(a => a.PreferredDateTime)
                .ToListAsync();

            var problems = await _db.DentalProblems.ToDictionaryAsync(p => p.Key, p => p.Name);

            return Ok(appts.Select(a => new
            {
                a.Id,
                a.FirstName,
                a.LastName,
                a.Email,
                a.PreferredDateTime,
                ProblemKey = a.ServiceNeeded,
                ProblemName = FormatServiceNames(a.ServiceNeeded, problems),
                a.Status
            }));
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? email, [FromQuery] string? service, [FromQuery] string? status, [FromQuery] int? dentistId, [FromQuery] bool? unassignedOnly)
        {
            var query = _db.Appointments.AsQueryable();

            if (!string.IsNullOrWhiteSpace(email))
                query = query.Where(a => a.Email.ToLower() == email.Trim().ToLower());

            if (!string.IsNullOrWhiteSpace(service))
                query = query.Where(a => a.ServiceNeeded.Contains(service.Trim()));

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(a => a.Status == status.Trim());

            if (dentistId.HasValue)
                query = query.Where(a => a.AssignedDentistUserId == dentistId.Value);

            if (unassignedOnly == true)
                query = query.Where(a => a.AssignedDentistUserId == null);

            var result = await query.OrderBy(a => a.PreferredDateTime).ToListAsync();
            return Ok(result);
        }

        [HttpGet("time-slots")]
        public async Task<IActionResult> GetTimeSlots([FromQuery] string date, [FromQuery] string services)
        {
            if (string.IsNullOrWhiteSpace(date) || string.IsNullOrWhiteSpace(services))
                return BadRequest(new { error = "Date and services are required." });

            var keys = AppointmentSchedulingService.ParseServiceKeys(services);
            if (keys.Count == 0)
                return BadRequest(new { error = "Select at least one treatment." });

            try
            {
                var slots = await _scheduling.GetTimeSlotsAsync(date.Trim(), keys);
                return Ok(slots);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException)
            {
                return BadRequest(new { error = "Invalid treatment selected." });
            }
        }

        [HttpGet("availability")]
        public async Task<IActionResult> CheckAvailability([FromQuery] string service, [FromQuery] string date, [FromQuery] string time)
        {
            var preferredDateTime = ParseDateTime(date, time);
            if (preferredDateTime == null)
                return BadRequest(new { error = "Invalid date or time." });

            var keys = AppointmentSchedulingService.ParseServiceKeys(service.Trim());
            if (keys.Count == 0)
                return BadRequest(new { error = "Select at least one treatment." });

            try
            {
                var available = await _scheduling.IsSlotAvailableAsync(keys, preferredDateTime.Value);
                return Ok(new { available });
            }
            catch (InvalidOperationException)
            {
                return BadRequest(new { error = "Invalid treatment selected." });
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var appt = await _db.Appointments.FindAsync(id);
            if (appt == null) return NotFound();
            return Ok(appt);
        }

        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateAppointmentStatusDto dto)
        {
            var appt = await _db.Appointments.FindAsync(id);
            if (appt == null) return NotFound();

            var allowed = new[] { "Booked", "InProgress", "Completed", "Cancelled" };
            if (!allowed.Contains(dto.Status))
                return BadRequest(new { error = "Invalid status." });

            appt.Status = dto.Status;
            await _db.SaveChangesAsync();
            await _email.SendStatusChangedAsync(appt, $"Appointment status changed to {dto.Status}.");
            return Ok(appt);
        }

        [HttpPost("{id:int}/cancel")]
        public async Task<IActionResult> Cancel(int id)
        {
            var appt = await _db.Appointments.FindAsync(id);
            if (appt == null) return NotFound();

            if (appt.PreferredDateTime <= DateTime.Now)
                return BadRequest(new { error = "Cannot cancel after appointment start time." });

            appt.Status = "Cancelled";
            await _db.SaveChangesAsync();
            await _email.SendAppointmentCancelledAsync(appt);
            return Ok(appt);
        }

        [HttpPatch("{id:int}/assign")]
        public async Task<IActionResult> AssignDentist(int id, [FromBody] AssignDentistDto dto)
        {
            var appt = await _db.Appointments.FindAsync(id);
            if (appt == null) return NotFound();

            var dentist = await _db.Users.FindAsync(dto.DentistUserId);
            if (dentist == null || dentist.Role != "Dentist")
                return BadRequest(new { error = "Invalid dentist." });

            var keys = AppointmentSchedulingService.ParseServiceKeys(appt.ServiceNeeded);
            var problems = await _db.DentalProblems.Where(p => keys.Contains(p.Key)).ToListAsync();
            if (problems.Count == 0)
                return BadRequest(new { error = "Invalid treatments on appointment." });

            if (problems.Any(p => p.DentistCategoryKey != dentist.DentistServiceKey))
                return BadRequest(new { error = "This dentist does not handle all selected treatments." });

            var duration = problems.Sum(p => p.DurationMinutes);
            var start = appt.PreferredDateTime;
            var end = start.AddMinutes(duration);
            var allProblems = await _db.DentalProblems.ToDictionaryAsync(p => p.Key, p => p.DurationMinutes);

            var otherAppts = await _db.Appointments
                .Where(a => a.Id != id && a.Status != "Cancelled" && a.AssignedDentistUserId == dentist.Id)
                .Where(a => a.PreferredDateTime.Date == start.Date)
                .ToListAsync();

            foreach (var other in otherAppts)
            {
                var otherKeys = AppointmentSchedulingService.ParseServiceKeys(other.ServiceNeeded);
                var otherDuration = otherKeys.Where(allProblems.ContainsKey).Sum(k => allProblems[k]);
                if (otherDuration <= 0) otherDuration = 60;
                var otherEnd = other.PreferredDateTime.AddMinutes(otherDuration);
                if (start < otherEnd && other.PreferredDateTime < end)
                    return BadRequest(new { error = "Dentist is not available at this time." });
            }

            appt.AssignedDentistUserId = dentist.Id;
            await _db.SaveChangesAsync();
            await _email.SendAppointmentAssignedAsync(appt, dentist);
            return Ok(appt);
        }

        [HttpPut("{id:int}/reschedule")]
        public async Task<IActionResult> Reschedule(int id, [FromBody] CreateAppointmentDto dto)
        {
            var appt = await _db.Appointments.FindAsync(id);
            if (appt == null) return NotFound();

            var preferredDateTime = ParseDateTime(dto.PreferredDate, dto.PreferredTime);
            if (preferredDateTime == null)
                return BadRequest(new { error = "Invalid date or time format." });

            var serviceKeys = string.IsNullOrWhiteSpace(dto.ServiceNeeded)
                ? AppointmentSchedulingService.ParseServiceKeys(appt.ServiceNeeded)
                : AppointmentSchedulingService.ParseServiceKeys(dto.ServiceNeeded.Trim());

            if (!await _scheduling.IsSlotAvailableAsync(serviceKeys, preferredDateTime.Value, id))
                return BadRequest(new { error = "Selected time slot is not available." });

            appt.PreferredDateTime = preferredDateTime.Value;
            appt.ServiceNeeded = string.Join(",", serviceKeys);
            appt.Status = "Booked";
            await _db.SaveChangesAsync();
            return Ok(appt);
        }

        private static string FormatServiceNames(string serviceNeeded, Dictionary<string, string> problems)
        {
            var keys = AppointmentSchedulingService.ParseServiceKeys(serviceNeeded);
            return string.Join(", ", keys.Select(k => problems.GetValueOrDefault(k, k)));
        }

        private static DateTime? ParseDateTime(string date, string time)
        {
            var dateFormats = new[] { "dd.MM.yyyy", "d.M.yyyy", "yyyy-MM-dd" };
            var timeFormats = new[] { "HH:mm", "H:mm" };

            if (!DateTime.TryParseExact(date, dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var datePart))
                return null;

            if (!DateTime.TryParseExact(time, timeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var timePart))
                return null;

            return new DateTime(datePart.Year, datePart.Month, datePart.Day, timePart.Hour, timePart.Minute, 0, DateTimeKind.Local);
        }
    }
}
