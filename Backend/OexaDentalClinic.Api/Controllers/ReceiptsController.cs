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
        private readonly ReceiptSyncService _receiptSync;

        public ReceiptsController(AppDbContext db, IEmailService email, ReceiptSyncService receiptSync)
        {
            _db = db;
            _email = email;
            _receiptSync = receiptSync;
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
            var appt = await _db.Appointments.FindAsync(appointmentId);
            if (appt == null) return NotFound();

            var receipt = await _db.Receipts.FirstOrDefaultAsync(r => r.AppointmentId == appointmentId);
            if (receipt == null)
            {
                var keys = AppointmentSchedulingService.ParseServiceKeys(appt.ServiceNeeded ?? "");
                if (keys.Count == 0)
                    return NotFound(new { error = "Appointment has no treatments to price." });

                receipt = new Receipt
                {
                    AppointmentId = appointmentId,
                    ReceiptNumber = "R-" + DateTime.UtcNow.Ticks,
                    Status = "PendingPricing",
                    TotalAmount = 0
                };
                _db.Receipts.Add(receipt);
                await _db.SaveChangesAsync();
            }

            await _receiptSync.EnsureAndSyncTreatmentLinesAsync(receipt, appt);

            var meds = await _db.ReceiptMedications
                .Where(m => m.ReceiptId == receipt.Id)
                .OrderBy(m => m.SubmittedByDentistUserId)
                .ThenBy(m => m.Id)
                .ToListAsync();

            var treatments = await _db.ReceiptTreatments
                .Where(t => t.ReceiptId == receipt.Id)
                .OrderBy(t => t.Id)
                .ToListAsync();

            var dentistIds = meds.Where(m => m.SubmittedByDentistUserId.HasValue).Select(m => m.SubmittedByDentistUserId!.Value)
                .Concat(treatments.Where(t => t.DentistUserId.HasValue).Select(t => t.DentistUserId!.Value))
                .Distinct()
                .ToList();
            var dentists = await _db.Users.Where(u => dentistIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id);

            var medicationDtos = meds.Select(m => new
            {
                m.Id,
                m.Name,
                m.UnitPrice,
                m.SubmittedByDentistUserId,
                dentistName = FormatDentistName(dentists, m.SubmittedByDentistUserId)
            }).ToList();

            var medicationsByDentist = medicationDtos
                .GroupBy(m => m.SubmittedByDentistUserId)
                .OrderBy(g => g.Key ?? int.MaxValue)
                .Select(g => new
                {
                    dentistUserId = g.Key,
                    dentistName = g.Key.HasValue
                        ? FormatDentistName(dentists, g.Key) ?? "Dentist"
                        : "Unassigned",
                    medications = g.ToList()
                })
                .ToList();

            return Ok(new
            {
                receipt,
                treatments = treatments.Select(t => new
                {
                    t.Id,
                    t.AppointmentTreatmentId,
                    t.ProblemKey,
                    t.Name,
                    t.UnitPrice,
                    t.DentistUserId,
                    dentistName = FormatDentistName(dentists, t.DentistUserId),
                    suggestedPriceEur = t.UnitPrice
                }),
                medications = medicationDtos,
                medicationsByDentist
            });
        }

        [HttpPost("medications")]
        public async Task<IActionResult> SubmitMedications([FromBody] SubmitReceiptMedicationsDto dto)
        {
            var appt = await _db.Appointments.FindAsync(dto.AppointmentId);
            if (appt == null) return NotFound();
            if (appt.Status == "Cancelled" || appt.Status == "Booked")
                return BadRequest(new { error = "Appointment must be checked in before submitting medications." });

            if (dto.DentistUserId <= 0)
                return BadRequest(new { error = "Dentist user id is required." });

            var isAssigned = await _db.AppointmentTreatments.AnyAsync(t =>
                t.AppointmentId == dto.AppointmentId && t.AssignedDentistUserId == dto.DentistUserId);

            if (!isAssigned)
                return BadRequest(new { error = "You are not assigned to this appointment." });

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

            var apptForSync = await _db.Appointments.FindAsync(dto.AppointmentId);
            if (apptForSync != null)
                await _receiptSync.EnsureAndSyncTreatmentLinesAsync(receipt, apptForSync);

            var old = await _db.ReceiptMedications
                .Where(m => m.ReceiptId == receipt.Id && m.SubmittedByDentistUserId == dto.DentistUserId)
                .ToListAsync();
            _db.ReceiptMedications.RemoveRange(old);

            foreach (var name in dto.Medications.Where(m => !string.IsNullOrWhiteSpace(m)))
            {
                _db.ReceiptMedications.Add(new ReceiptMedication
                {
                    ReceiptId = receipt.Id,
                    Name = name.Trim(),
                    SubmittedByDentistUserId = dto.DentistUserId
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
            foreach (var line in dto.MedicationLines)
            {
                var med = meds.FirstOrDefault(m => m.Id == line.MedicationId);
                if (med != null) med.UnitPrice = line.UnitPrice;
            }

            var treatments = await _db.ReceiptTreatments.Where(t => t.ReceiptId == receiptId).ToListAsync();
            foreach (var line in dto.TreatmentLines)
            {
                var tr = treatments.FirstOrDefault(t => t.AppointmentTreatmentId == line.TreatmentLineId)
                    ?? treatments.FirstOrDefault(t => t.Id == line.TreatmentLineId);
                if (tr != null) tr.UnitPrice = line.UnitPrice;
            }

            var medTotal = meds.Where(m => m.UnitPrice.HasValue).Sum(m => m.UnitPrice!.Value);
            var treatmentTotal = treatments.Where(t => t.UnitPrice.HasValue).Sum(t => t.UnitPrice!.Value);
            VatHelper.ApplyToReceipt(receipt, medTotal + treatmentTotal);
            receipt.Status = "Finalized";
            await _db.SaveChangesAsync();

            var appt = await _db.Appointments.FindAsync(receipt.AppointmentId);
            if (appt != null)
                await _email.SendReceiptFinalizedAsync(appt, receipt, meds, treatments);

            return Ok(new
            {
                receipt = new
                {
                    receipt.Id,
                    receipt.ReceiptNumber,
                    receipt.AppointmentId,
                    receipt.Status,
                    receipt.SubtotalBeforeVat,
                    receipt.VatAmount,
                    receipt.TotalAmount,
                    vatRatePercent = VatHelper.RatePercent
                },
                medications = meds,
                treatments,
                subtotalBeforeVat = receipt.SubtotalBeforeVat,
                vatAmount = receipt.VatAmount,
                totalAfterVat = receipt.TotalAmount
            });
        }

        private static string? FormatDentistName(Dictionary<int, User> dentists, int? dentistId)
        {
            if (!dentistId.HasValue || !dentists.TryGetValue(dentistId.Value, out var d))
                return null;
            return $"Dr. {d.FirstName} {d.LastName}";
        }
    }
}
