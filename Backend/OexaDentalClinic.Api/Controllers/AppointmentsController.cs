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

        public AppointmentsController(AppDbContext db, IEmailService email)
        {
            _db = db;
            _email = email;
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

            if (await IsSlotTaken(dto.ServiceNeeded.Trim(), preferredDateTime.Value))
                return BadRequest(new { error = "Selected time slot is not available." });

            var service = dto.ServiceNeeded.Trim();
            var dentist = await _db.Users.FirstOrDefaultAsync(u =>
                u.Role == "Dentist" && u.DentistServiceKey == service);

            var appointment = new Appointment
            {
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                Email = dto.Email.Trim(),
                PhoneNumber = dto.PhoneNumber.Trim(),
                PreferredDateTime = preferredDateTime.Value,
                ServiceNeeded = service,
                AdditionalNotes = string.IsNullOrWhiteSpace(dto.AdditionalNotes) ? null : dto.AdditionalNotes.Trim(),
                IsSpecialAppointment = dto.IsSpecialAppointment,
                PatientUserId = dto.PatientUserId,
                AssignedDentistUserId = dentist?.Id,
                Status = "Booked"
            };

            _db.Appointments.Add(appointment);
            await _db.SaveChangesAsync();

            await _email.SendAppointmentConfirmationAsync(appointment);

            return CreatedAtAction(nameof(GetById), new { id = appointment.Id }, appointment);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? email, [FromQuery] string? service, [FromQuery] string? status, [FromQuery] int? dentistId)
        {
            var query = _db.Appointments.AsQueryable();

            if (!string.IsNullOrWhiteSpace(email))
                query = query.Where(a => a.Email.ToLower() == email.Trim().ToLower());

            if (!string.IsNullOrWhiteSpace(service))
                query = query.Where(a => a.ServiceNeeded == service.Trim());

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(a => a.Status == status.Trim());

            if (dentistId.HasValue)
                query = query.Where(a => a.AssignedDentistUserId == dentistId.Value);

            var result = await query.OrderBy(a => a.PreferredDateTime).ToListAsync();
            return Ok(result);
        }

        [HttpGet("availability")]
        public async Task<IActionResult> CheckAvailability([FromQuery] string service, [FromQuery] string date, [FromQuery] string time)
        {
            var preferredDateTime = ParseDateTime(date, time);
            if (preferredDateTime == null)
                return BadRequest(new { error = "Invalid date or time." });

            var available = !await IsSlotTaken(service.Trim(), preferredDateTime.Value);
            return Ok(new { available });
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

            appt.AssignedDentistUserId = dentist.Id;
            appt.ServiceNeeded = dentist.DentistServiceKey ?? appt.ServiceNeeded;
            await _db.SaveChangesAsync();
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

            var service = string.IsNullOrWhiteSpace(dto.ServiceNeeded) ? appt.ServiceNeeded : dto.ServiceNeeded.Trim();

            if (await _db.Appointments.AnyAsync(a =>
                a.Id != id &&
                a.ServiceNeeded == service &&
                a.PreferredDateTime == preferredDateTime.Value &&
                a.Status != "Cancelled"))
                return BadRequest(new { error = "Selected time slot is not available." });

            appt.PreferredDateTime = preferredDateTime.Value;
            appt.ServiceNeeded = service;
            appt.Status = "Booked";
            await _db.SaveChangesAsync();
            return Ok(appt);
        }

        private async Task<bool> IsSlotTaken(string service, DateTime dateTime)
        {
            return await _db.Appointments.AnyAsync(a =>
                a.ServiceNeeded == service &&
                a.PreferredDateTime == dateTime &&
                a.Status != "Cancelled");
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
