using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OexaDentalClinic.Api.Data;
using OexaDentalClinic.Api.DTOs;
using OexaDentalClinic.Api.Models;
using OexaDentalClinic.Api.Services;

namespace OexaDentalClinic.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReceiptsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IEmailService _email;

        public ReceiptsController(AppDbContext db, IEmailService email)
        {
            _db = db;
            _email = email;
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingPricing()
        {
            var items = await _db.Receipts
                .Where(r => r.Status == "PendingPricing")
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new { r.Id, r.AppointmentId, r.ReceiptNumber, r.CreatedAt })
                .ToListAsync();
            return Ok(items);
        }

        [HttpGet("{appointmentId:int}")]
        public async Task<IActionResult> GetByAppointment(int appointmentId)
        {
            var receipt = await _db.Receipts.FirstOrDefaultAsync(r => r.AppointmentId == appointmentId);
            if (receipt == null) return NotFound();

            var meds = await _db.ReceiptMedications
                .Where(m => m.ReceiptId == receipt.Id)
                .OrderBy(m => m.Id)
                .ToListAsync();

            return Ok(new { receipt, medications = meds });
        }

        [HttpPost("medications")]
        public async Task<IActionResult> SubmitMedications([FromBody] SubmitReceiptMedicationsDto dto)
        {
            var appt = await _db.Appointments.FindAsync(dto.AppointmentId);
            if (appt == null) return NotFound();
            if (appt.Status != "Completed" && appt.Status != "InProgress")
                return BadRequest(new { error = "Appointment must be in progress or completed." });

            var receipt = await _db.Receipts.FirstOrDefaultAsync(r => r.AppointmentId == dto.AppointmentId);
            if (receipt == null)
            {
                receipt = new Receipt
                {
                    AppointmentId = dto.AppointmentId,
                    ReceiptNumber = "R-" + DateTime.UtcNow.Ticks,
                    Status = "PendingPricing",
                    TotalAmount = 0
                };
                _db.Receipts.Add(receipt);
                await _db.SaveChangesAsync();
            }
            else
            {
                var old = await _db.ReceiptMedications.Where(m => m.ReceiptId == receipt.Id).ToListAsync();
                _db.ReceiptMedications.RemoveRange(old);
            }

            foreach (var name in dto.Medications.Where(m => !string.IsNullOrWhiteSpace(m)))
            {
                _db.ReceiptMedications.Add(new ReceiptMedication
                {
                    ReceiptId = receipt.Id,
                    Name = name.Trim()
                });
            }

            receipt.Status = "PendingPricing";
            await _db.SaveChangesAsync();

            await _email.SendReceiptPendingPricingAsync(appt, receipt);

            return Ok(new { receipt.Id, receipt.ReceiptNumber, receipt.Status });
        }

        [HttpPut("{receiptId:int}/prices")]
        public async Task<IActionResult> SetPrices(int receiptId, [FromBody] PriceReceiptDto dto)
        {
            var receipt = await _db.Receipts.FindAsync(receiptId);
            if (receipt == null) return NotFound();

            var meds = await _db.ReceiptMedications.Where(m => m.ReceiptId == receiptId).ToListAsync();
            foreach (var line in dto.Lines)
            {
                var med = meds.FirstOrDefault(m => m.Id == line.MedicationId);
                if (med != null) med.UnitPrice = line.UnitPrice;
            }

            receipt.TotalAmount = meds.Where(m => m.UnitPrice.HasValue).Sum(m => m.UnitPrice!.Value);
            receipt.Status = "Finalized";
            await _db.SaveChangesAsync();

            var appt = await _db.Appointments.FindAsync(receipt.AppointmentId);
            if (appt != null)
                await _email.SendReceiptFinalizedAsync(appt, receipt, meds);

            return Ok(new { receipt, medications = meds });
        }
    }
}
