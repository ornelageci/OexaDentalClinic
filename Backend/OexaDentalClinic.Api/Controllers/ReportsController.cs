using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OexaDentalClinic.Api.Data;
using OexaDentalClinic.Api.Services;

namespace OexaDentalClinic.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ReceiptSyncService _receiptSync;

        public ReportsController(AppDbContext db, ReceiptSyncService receiptSync)
        {
            _db = db;
            _receiptSync = receiptSync;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var now = DateTime.Now;
            return await GetRevenue(null, now.Year, now.Month);
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var now = DateTime.Now;
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var monthEnd = monthStart.AddMonths(1);

            var totalAppointments = await _db.Appointments.CountAsync();
            var completed = await _db.Appointments.CountAsync(a => a.Status == "Completed");
            var cancelled = await _db.Appointments.CountAsync(a => a.Status == "Cancelled");
            var inProgress = await _db.Appointments.CountAsync(a => a.Status == "InProgress");
            var booked = await _db.Appointments.CountAsync(a => a.Status == "Booked");

            var monthRevenue = await (
                from r in _db.Receipts
                join a in _db.Appointments on r.AppointmentId equals a.Id
                where r.Status == "Finalized"
                      && a.Status == "Completed"
                      && a.PreferredDateTime >= monthStart
                      && a.PreferredDateTime < monthEnd
                select r.TotalAmount
            ).SumAsync();

            var receiptCount = await (
                from r in _db.Receipts
                join a in _db.Appointments on r.AppointmentId equals a.Id
                where r.Status == "Finalized"
                      && a.Status == "Completed"
                      && a.PreferredDateTime >= monthStart
                      && a.PreferredDateTime < monthEnd
                select r.Id
            ).CountAsync();

            return Ok(new
            {
                totalAppointments,
                completed,
                cancelled,
                inProgress,
                booked,
                monthLabel = monthStart.ToString("MMMM yyyy"),
                monthRevenue = Math.Round(monthRevenue, 2),
                receiptCountThisMonth = receiptCount
            });
        }

        [HttpGet("revenue")]
        public async Task<IActionResult> GetRevenue(
            [FromQuery] string? period,
            [FromQuery] int? year,
            [FromQuery] int? month)
        {
            var now = DateTime.Now;
            int y;
            int? monthOut = null;
            string periodKind;

            if (year.HasValue)
            {
                y = year.Value;
                if (y < 2000 || y > 2100)
                    return BadRequest(new { error = "Year must be between 2000 and 2100." });
                if (!month.HasValue)
                    return BadRequest(new { error = "Month is required (0 = full year, 1–12 = month)." });

                if (month.Value == 0)
                    periodKind = "year";
                else if (month is >= 1 and <= 12)
                {
                    monthOut = month.Value;
                    periodKind = "month";
                }
                else
                    return BadRequest(new { error = "Month must be 0 (full year) or 1–12." });
            }
            else if (!string.IsNullOrWhiteSpace(period))
            {
                var parsed = TryParsePeriodCode(period);
                if (parsed == null)
                    return BadRequest(new { error = "Period must be YYYY-MM (e.g. 2026-06 for June, 2026-00 for full year)." });
                (y, monthOut, periodKind) = parsed.Value;
            }
            else
            {
                y = now.Year;
                monthOut = now.Month;
                periodKind = "month";
            }

            DateTime periodStart;
            DateTime periodEnd;
            string periodLabel;
            string periodCode;

            if (periodKind == "month" && monthOut is >= 1 and <= 12)
            {
                periodCode = $"{y}-{monthOut.Value:D2}";
                periodStart = new DateTime(y, monthOut.Value, 1);
                periodEnd = periodStart.AddMonths(1);
                periodLabel = periodStart.ToString("MMMM yyyy");
            }
            else
            {
                monthOut = null;
                periodKind = "year";
                periodCode = $"{y}-00";
                periodStart = new DateTime(y, 1, 1);
                periodEnd = periodStart.AddYears(1);
                periodLabel = y.ToString();
            }

            var rows = await (
                from r in _db.Receipts
                join a in _db.Appointments on r.AppointmentId equals a.Id
                where r.Status == "Finalized"
                      && a.Status == "Completed"
                      && a.PreferredDateTime >= periodStart
                      && a.PreferredDateTime < periodEnd
                orderby a.PreferredDateTime descending
                select new { Receipt = r, Appointment = a }
            ).ToListAsync();

            if (rows.Count == 0)
            {
                return Ok(new
                {
                    year = y,
                    month = monthOut,
                    periodCode,
                    periodKind,
                    periodLabel,
                    monthLabel = periodLabel,
                    vatRatePercent = VatHelper.RatePercent,
                    summary = new
                    {
                        receiptCount = 0,
                        completedVisits = 0,
                        subtotalBeforeVat = 0m,
                        vatAmount = 0m,
                        totalAfterVat = 0m,
                        treatmentsTotal = 0m,
                        medicationsTotal = 0m
                    },
                    byDentist = Array.Empty<object>(),
                    receipts = Array.Empty<object>()
                });
            }

            foreach (var row in rows)
                await _receiptSync.EnsureAndSyncTreatmentLinesAsync(row.Receipt, row.Appointment);

            var receiptIds = rows.Select(x => x.Receipt.Id).ToList();
            var apptIds = rows.Select(x => x.Appointment.Id).Distinct().ToList();

            var treatments = await _db.ReceiptTreatments
                .Where(t => receiptIds.Contains(t.ReceiptId))
                .ToListAsync();
            var medications = await _db.ReceiptMedications
                .Where(m => receiptIds.Contains(m.ReceiptId))
                .ToListAsync();

            var dentists = await _db.Users
                .Where(u => u.Role == "Dentist")
                .ToDictionaryAsync(u => u.Id, u => $"Dr. {u.FirstName} {u.LastName}");

            var receiptDtos = new List<object>();
            decimal sumSubtotal = 0, sumVat = 0, sumTotal = 0, sumTreat = 0, sumMeds = 0;

            foreach (var row in rows)
            {
                var r = row.Receipt;
                var a = row.Appointment;
                var rTreatments = treatments.Where(t => t.ReceiptId == r.Id).ToList();
                var rMeds = medications.Where(m => m.ReceiptId == r.Id).ToList();

                var treatSum = rTreatments.Where(t => t.UnitPrice.HasValue).Sum(t => t.UnitPrice!.Value);
                var medSum = rMeds.Where(m => m.UnitPrice.HasValue).Sum(m => m.UnitPrice!.Value);
                var lineSubtotal = treatSum + medSum;

                decimal subtotal, vat, total;
                if (r.SubtotalBeforeVat > 0 || r.VatAmount > 0)
                {
                    subtotal = r.SubtotalBeforeVat;
                    vat = r.VatAmount;
                    total = r.TotalAmount;
                }
                else if (r.TotalAmount > 0 && lineSubtotal <= 0)
                {
                    (subtotal, vat, total) = VatHelper.FromTotalIncludingVat(r.TotalAmount);
                }
                else
                {
                    (subtotal, vat, total) = VatHelper.FromSubtotal(lineSubtotal);
                }

                sumSubtotal += subtotal;
                sumVat += vat;
                sumTotal += total;
                sumTreat += treatSum;
                sumMeds += medSum;

                receiptDtos.Add(new
                {
                    receiptId = r.Id,
                    receiptNumber = r.ReceiptNumber,
                    appointmentId = a.Id,
                    patientName = $"{a.FirstName} {a.LastName}",
                    visitDate = a.PreferredDateTime,
                    treatments = rTreatments.Select(t => new
                    {
                        t.Name,
                        dentistName = DentistName(dentists, t.DentistUserId),
                        amountEur = t.UnitPrice ?? 0
                    }),
                    medications = rMeds.Select(m => new
                    {
                        m.Name,
                        dentistName = DentistName(dentists, m.SubmittedByDentistUserId),
                        amountEur = m.UnitPrice ?? 0
                    }),
                    treatmentsTotalEur = treatSum,
                    medicationsTotalEur = medSum,
                    subtotalBeforeVat = subtotal,
                    vatAmount = vat,
                    vatRatePercent = VatHelper.RatePercent,
                    totalAfterVat = total
                });
            }

            var byDentistMap = new Dictionary<int, DentistRevenueAccumulator>();

            foreach (var t in treatments)
            {
                if (!t.DentistUserId.HasValue || !t.UnitPrice.HasValue) continue;
                var acc = GetOrAdd(byDentistMap, t.DentistUserId.Value, dentists);
                acc.Treatments += t.UnitPrice.Value;
            }

            foreach (var med in medications)
            {
                if (!med.SubmittedByDentistUserId.HasValue || !med.UnitPrice.HasValue) continue;
                var acc = GetOrAdd(byDentistMap, med.SubmittedByDentistUserId.Value, dentists);
                acc.Medications += med.UnitPrice.Value;
            }

            var byDentist = byDentistMap.Values
                .Select(acc =>
                {
                    var (sub, vat, tot) = VatHelper.FromSubtotal(acc.Treatments + acc.Medications);
                    return new
                    {
                        acc.DentistId,
                        acc.DentistName,
                        treatmentsTotalEur = acc.Treatments,
                        medicationsTotalEur = acc.Medications,
                        subtotalBeforeVat = sub,
                        vatAmount = vat,
                        totalAfterVat = tot
                    };
                })
                .OrderByDescending(x => x.totalAfterVat)
                .ToList();

            return Ok(new
            {
                year = y,
                month = monthOut,
                periodCode,
                periodKind,
                periodLabel,
                monthLabel = periodLabel,
                vatRatePercent = VatHelper.RatePercent,
                summary = new
                {
                    receiptCount = rows.Count,
                    completedVisits = apptIds.Count,
                    subtotalBeforeVat = Math.Round(sumSubtotal, 2),
                    vatAmount = Math.Round(sumVat, 2),
                    totalAfterVat = Math.Round(sumTotal, 2),
                    treatmentsTotal = Math.Round(sumTreat, 2),
                    medicationsTotal = Math.Round(sumMeds, 2)
                },
                byDentist,
                receipts = receiptDtos
            });
        }

        /// <summary>YYYY-MM — use MM 01–12 for a month, 00 for the full year.</summary>
        private static (int Year, int? Month, string Kind)? TryParsePeriodCode(string period)
        {
            var parts = period.Trim().Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
                return null;
            if (!int.TryParse(parts[0], out var y) || y < 2000 || y > 2100)
                return null;
            if (!int.TryParse(parts[1], out var m) || m < 0 || m > 12)
                return null;

            if (m == 0)
                return (y, null, "year");

            return (y, m, "month");
        }

        private static string DentistName(Dictionary<int, string> dentists, int? id)
        {
            if (!id.HasValue) return "—";
            return dentists.GetValueOrDefault(id.Value, "Dentist");
        }

        private static DentistRevenueAccumulator GetOrAdd(
            Dictionary<int, DentistRevenueAccumulator> map,
            int dentistId,
            Dictionary<int, string> dentists)
        {
            if (!map.TryGetValue(dentistId, out var acc))
            {
                acc = new DentistRevenueAccumulator
                {
                    DentistId = dentistId,
                    DentistName = dentists.GetValueOrDefault(dentistId, "Dentist")
                };
                map[dentistId] = acc;
            }

            return acc;
        }

        private sealed class DentistRevenueAccumulator
        {
            public int DentistId { get; set; }
            public string DentistName { get; set; } = "";
            public decimal Treatments { get; set; }
            public decimal Medications { get; set; }
        }
    }
}
