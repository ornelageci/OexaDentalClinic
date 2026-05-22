using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OexaDentalClinic.Api.Data;
using OexaDentalClinic.Api.DTOs;
using OexaDentalClinic.Api.Models;

namespace OexaDentalClinic.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TreatmentsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public TreatmentsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("{appointmentId:int}")]
        public async Task<IActionResult> GetByAppointment(int appointmentId)
        {
            var record = await _db.TreatmentRecords.FirstOrDefaultAsync(t => t.AppointmentId == appointmentId);
            if (record == null) return NotFound();
            return Ok(record);
        }

        [HttpPut("{appointmentId:int}")]
        public async Task<IActionResult> Upsert(int appointmentId, [FromBody] UpdateTreatmentDto dto)
        {
            var appt = await _db.Appointments.FindAsync(appointmentId);
            if (appt == null) return NotFound();

            var record = await _db.TreatmentRecords.FirstOrDefaultAsync(t => t.AppointmentId == appointmentId);
            if (record == null)
            {
                record = new TreatmentRecord { AppointmentId = appointmentId };
                _db.TreatmentRecords.Add(record);
            }

            record.Diagnosis = dto.Diagnosis?.Trim();
            record.TreatmentPerformed = dto.TreatmentPerformed?.Trim();
            record.Recommendations = dto.Recommendations?.Trim();
            record.MedicationPrescribed = dto.MedicationPrescribed?.Trim();
            record.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok(record);
        }
    }
}
