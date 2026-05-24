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
            var appt = await _db.Appointments.FindAsync(appointmentId);
            if (appt == null) return NotFound();

            var receipt = await _db.Receipts.FirstOrDefaultAsync(r => r.AppointmentId == appointmentId);
            if (receipt == null)
            {
                var hasLines = await _db.AppointmentTreatments.AnyAsync(t => t.AppointmentId == appointmentId);
                if (!hasLines)
                    return NotFound();

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

            await SyncReceiptTreatmentsAsync(receipt, appointmentId);

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
                medications = meds.Select(m => new
                {
                    m.Id,
                    m.Name,
                    m.UnitPrice,
                    m.SubmittedByDentistUserId,
                    dentistName = FormatDentistName(dentists, m.SubmittedByDentistUserId)
                })
            });
        }

        [HttpPost("medications")]
        public async Task<IActionResult> SubmitMedications([FromBody] SubmitReceiptMedicationsDto dto)
        {
            var appt = await _db.Appointments.FindAsync(dto.AppointmentId);
            if (appt == null) return NotFound();
            if (appt.Status == "Cancelled" || appt.Status == "Booked")
                return BadRequest(new { error = "Appointment must be checked in before submitting medications." });

            var myLines = await _db.AppointmentTreatments
                .Where(t => t.AppointmentId == dto.AppointmentId && t.AssignedDentistUserId == dto.DentistUserId)
                .ToListAsync();

            if (myLines.Count == 0)
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
                await SyncReceiptTreatmentsAsync(receipt, dto.AppointmentId);
            }

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
                var tr = treatments.FirstOrDefault(t => t.AppointmentTreatmentId == line.TreatmentLineId);
                if (tr != null) tr.UnitPrice = line.UnitPrice;
            }

            var medTotal = meds.Where(m => m.UnitPrice.HasValue).Sum(m => m.UnitPrice!.Value);
            var treatmentTotal = treatments.Where(t => t.UnitPrice.HasValue).Sum(t => t.UnitPrice!.Value);
            receipt.TotalAmount = medTotal + treatmentTotal;
            receipt.Status = "Finalized";
            await _db.SaveChangesAsync();

            var appt = await _db.Appointments.FindAsync(receipt.AppointmentId);
            if (appt != null)
                await _email.SendReceiptFinalizedAsync(appt, receipt, meds, treatments);

            return Ok(new
            {
                receipt,
                medications = meds,
                treatments,
                totalEur = receipt.TotalAmount
            });
        }

        private async Task SyncReceiptTreatmentsAsync(Receipt receipt, int appointmentId)
        {
            var lines = await _db.AppointmentTreatments
                .Where(t => t.AppointmentId == appointmentId)
                .ToListAsync();
            if (lines.Count == 0) return;

            var existing = await _db.ReceiptTreatments
                .Where(t => t.ReceiptId == receipt.Id)
                .ToListAsync();

            var problems = await DentalProblemLookup.LoadAllAsync(_db);
            var allPromos = await _db.Promotions.Where(p => p.IsActive && p.ProblemKey != null).AsNoTracking().ToListAsync();
            var activePromos = allPromos.Where(p => PromotionHelper.IsActiveOnDate(p)).ToList();

            foreach (var line in lines)
            {
                if (existing.Any(e => e.AppointmentTreatmentId == line.Id))
                    continue;

                var problem = DentalProblemLookup.Find(problems, line.ProblemKey);
                var name = problem?.Name ?? line.ProblemKey;
                decimal? suggested = null;
                if (problem != null)
                {
                    var promo = activePromos.FirstOrDefault(x => PromotionHelper.KeysMatch(x.ProblemKey, problem.Key));
                    suggested = promo != null
                        ? Math.Round(problem.BasePrice * (100 - promo.DiscountPercent) / 100m, 2)
                        : problem.BasePrice;
                }

                _db.ReceiptTreatments.Add(new ReceiptTreatment
                {
                    ReceiptId = receipt.Id,
                    AppointmentTreatmentId = line.Id,
                    ProblemKey = line.ProblemKey,
                    Name = name,
                    DentistUserId = line.AssignedDentistUserId,
                    UnitPrice = suggested
                });
            }

            await _db.SaveChangesAsync();
        }

        private async Task<List<object>> BuildTreatmentPreviewAsync(int appointmentId, Receipt? receipt)
        {
            var lines = await _db.AppointmentTreatments
                .Where(t => t.AppointmentId == appointmentId)
                .ToListAsync();
            if (lines.Count == 0) return new List<object>();

            var problems = await DentalProblemLookup.LoadAllAsync(_db);
            var allPromos = await _db.Promotions.Where(p => p.IsActive && p.ProblemKey != null).AsNoTracking().ToListAsync();
            var activePromos = allPromos.Where(p => PromotionHelper.IsActiveOnDate(p)).ToList();
            var dentistIds = lines.Where(l => l.AssignedDentistUserId.HasValue).Select(l => l.AssignedDentistUserId!.Value).Distinct().ToList();
            var dentists = await _db.Users.Where(u => dentistIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id);

            return lines.Select(line =>
            {
                var problem = DentalProblemLookup.Find(problems, line.ProblemKey);
                var name = problem?.Name ?? line.ProblemKey;
                decimal? suggested = null;
                if (problem != null)
                {
                    var promo = activePromos.FirstOrDefault(x => PromotionHelper.KeysMatch(x.ProblemKey, problem.Key));
                    suggested = promo != null
                        ? Math.Round(problem.BasePrice * (100 - promo.DiscountPercent) / 100m, 2)
                        : problem.BasePrice;
                }

                return (object)new
                {
                    appointmentTreatmentId = line.Id,
                    line.ProblemKey,
                    name,
                    dentistUserId = line.AssignedDentistUserId,
                    dentistName = FormatDentistName(dentists, line.AssignedDentistUserId),
                    suggestedPriceEur = suggested,
                    unitPrice = suggested
                };
            }).ToList();
        }

        private static string? FormatDentistName(Dictionary<int, User> dentists, int? dentistId)
        {
            if (!dentistId.HasValue || !dentists.TryGetValue(dentistId.Value, out var d))
                return null;
            return $"Dr. {d.FirstName} {d.LastName}";
        }
    }
}
