using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OexaDentalClinic.Api.Data;
using OexaDentalClinic.Api.DTOs;
using OexaDentalClinic.Api.Models;

namespace OexaDentalClinic.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReceiptsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ReceiptsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _db.Receipts.OrderByDescending(r => r.CreatedAt).ToListAsync());
        }

        [HttpGet("{appointmentId:int}")]
        public async Task<IActionResult> GetByAppointment(int appointmentId)
        {
            var receipt = await _db.Receipts.FirstOrDefaultAsync(r => r.AppointmentId == appointmentId);
            if (receipt == null) return NotFound();
            return Ok(receipt);
        }

        [HttpPost]
        public async Task<IActionResult> Finalize([FromBody] CreateReceiptDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var appt = await _db.Appointments.FindAsync(dto.AppointmentId);
            if (appt == null) return NotFound();

            var existing = await _db.Receipts.FirstOrDefaultAsync(r => r.AppointmentId == dto.AppointmentId);
            if (existing != null)
            {
                existing.TotalAmount = dto.TotalAmount;
                existing.Status = "Finalized";
                await _db.SaveChangesAsync();
                return Ok(existing);
            }

            var receipt = new Receipt
            {
                AppointmentId = dto.AppointmentId,
                ReceiptNumber = "R-" + DateTime.UtcNow.Ticks,
                TotalAmount = dto.TotalAmount,
                Status = "Finalized"
            };

            _db.Receipts.Add(receipt);
            await _db.SaveChangesAsync();
            return Ok(receipt);
        }
    }
}
