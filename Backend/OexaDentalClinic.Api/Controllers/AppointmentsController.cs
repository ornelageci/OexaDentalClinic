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

            var problems = await _scheduling.GetProblemsForKeysAsync(serviceKeys);
            await _scheduling.CreateTreatmentLinesAsync(appointment.Id, problems, preferredDateTime.Value);

            await _email.SendAppointmentBookedAsync(appointment);

            return CreatedAtAction(nameof(GetById), new { id = appointment.Id }, appointment);
        }

        [HttpGet("unassigned")]
        public async Task<IActionResult> GetUnassigned()
        {
            await EnsureTreatmentLinesForOpenAppointmentsAsync();

            var problems = await DentalProblemLookup.NameByKeyAsync(_db);

            var rows = await (
                from t in _db.AppointmentTreatments
                join a in _db.Appointments on t.AppointmentId equals a.Id
                where a.Status == "Booked" && t.AssignedDentistUserId == null
                orderby t.ScheduledStart, a.Id
                select new { t, a }
            ).ToListAsync();

            return Ok(rows.Select(r => new
            {
                appointmentId = r.a.Id,
                treatmentLineId = r.t.Id,
                r.a.FirstName,
                r.a.LastName,
                r.a.Email,
                preferredDateTime = r.a.PreferredDateTime,
                scheduledStart = r.t.ScheduledStart,
                problemKey = r.t.ProblemKey,
                problemName = problems.GetValueOrDefault(r.t.ProblemKey, r.t.ProblemKey),
                durationMinutes = r.t.DurationMinutes,
                r.a.Status
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
            {
                var did = dentistId.Value;
                var apptIdsForDentist = await _db.AppointmentTreatments
                    .Where(t => t.AssignedDentistUserId == did)
                    .Select(t => t.AppointmentId)
                    .Distinct()
                    .ToListAsync();
                query = query.Where(a =>
                    a.AssignedDentistUserId == did || apptIdsForDentist.Contains(a.Id));
            }

            if (unassignedOnly == true)
            {
                var fullyAssigned = await _db.AppointmentTreatments
                    .GroupBy(t => t.AppointmentId)
                    .Where(g => g.All(t => t.AssignedDentistUserId != null))
                    .Select(g => g.Key)
                    .ToListAsync();
                query = query.Where(a =>
                    a.AssignedDentistUserId == null && !fullyAssigned.Contains(a.Id));
            }

            var result = await query.OrderBy(a => a.PreferredDateTime).ToListAsync();

            if (!string.IsNullOrWhiteSpace(email))
            {
                var apptIds = result.Select(a => a.Id).ToList();
                var reviews = await _db.Reviews
                    .Where(r => apptIds.Contains(r.AppointmentId))
                    .ToDictionaryAsync(r => r.AppointmentId);
                var users = await _db.Users.AsNoTracking().ToListAsync();
                var problems = await DentalProblemLookup.NameByKeyAsync(_db);

                var enriched = result.Select(a =>
                {
                    var keys = AppointmentSchedulingService.ParseServiceKeys(a.ServiceNeeded);
                    var serviceNames = string.Join(", ", keys.Select(k => problems.GetValueOrDefault(k, k)));
                    User? dentist = null;
                    if (a.AssignedDentistUserId.HasValue)
                        dentist = users.FirstOrDefault(u => u.Id == a.AssignedDentistUserId.Value);
                    reviews.TryGetValue(a.Id, out var review);

                    return new
                    {
                        a.Id,
                        a.FirstName,
                        a.LastName,
                        a.Email,
                        a.PhoneNumber,
                        a.PreferredDateTime,
                        a.ServiceNeeded,
                        serviceNames,
                        a.AdditionalNotes,
                        a.Status,
                        a.IsSpecialAppointment,
                        a.AssignedDentistUserId,
                        dentistName = dentist != null ? $"Dr. {dentist.FirstName} {dentist.LastName}" : null,
                        hasReview = review != null,
                        reviewRating = review?.Rating,
                        reviewComment = review?.Comment
                    };
                });
                return Ok(enriched);
            }

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
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Could not load time slots.", detail = ex.Message });
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

        [HttpGet("{id:int}/details")]
        public async Task<IActionResult> GetDetails(int id)
        {
            var appt = await _db.Appointments.FindAsync(id);
            if (appt == null) return NotFound();

            var keys = AppointmentSchedulingService.ParseServiceKeys(appt.ServiceNeeded);
            var allProblems = await _db.DentalProblems.AsNoTracking().ToListAsync();
            var allPromos = await _db.Promotions
                .Where(p => p.IsActive && p.ProblemKey != null)
                .AsNoTracking()
                .ToListAsync();
            var activePromos = allPromos.Where(p => PromotionHelper.IsActiveOnDate(p)).ToList();

            var treatments = new List<object>();
            decimal estimatedBase = 0;
            decimal estimatedTotal = 0;

            foreach (var key in keys)
            {
                var problem = DentalProblemLookup.Find(allProblems, key);
                if (problem == null)
                {
                    treatments.Add(new { key, name = key });
                    continue;
                }

                var promo = activePromos.FirstOrDefault(x => PromotionHelper.KeysMatch(x.ProblemKey, problem.Key));
                var discounted = promo != null
                    ? Math.Round(problem.BasePrice * (100 - promo.DiscountPercent) / 100m, 2)
                    : (decimal?)null;
                var display = discounted ?? problem.BasePrice;
                estimatedBase += problem.BasePrice;
                estimatedTotal += display;

                treatments.Add(new
                {
                    key = problem.Key,
                    name = problem.Name,
                    durationMinutes = problem.DurationMinutes,
                    basePrice = problem.BasePrice,
                    discountPercent = promo?.DiscountPercent,
                    promotionTitle = promo?.Title,
                    priceAfterDiscount = discounted,
                    displayPrice = display
                });
            }

            User? dentist = null;
            if (appt.AssignedDentistUserId.HasValue)
                dentist = await _db.Users.FindAsync(appt.AssignedDentistUserId.Value);

            var receipt = await _db.Receipts.AsNoTracking()
                .FirstOrDefaultAsync(r => r.AppointmentId == id);
            object? receiptInfo = null;
            if (receipt != null)
            {
                var meds = await _db.ReceiptMedications.AsNoTracking()
                    .Where(m => m.ReceiptId == receipt.Id)
                    .OrderBy(m => m.Id)
                    .ToListAsync();
                receiptInfo = new
                {
                    receipt.Id,
                    receipt.ReceiptNumber,
                    receipt.Status,
                    receipt.TotalAmount,
                    isFinalized = receipt.Status == "Finalized",
                    medications = meds.Select(m => new { m.Id, m.Name, m.UnitPrice })
                };
            }

            var treatmentRecord = await _db.TreatmentRecords.AsNoTracking()
                .FirstOrDefaultAsync(t => t.AppointmentId == id);

            return Ok(new
            {
                appointment = new
                {
                    appt.Id,
                    appt.FirstName,
                    appt.LastName,
                    appt.Email,
                    appt.PhoneNumber,
                    appt.PreferredDateTime,
                    appt.ServiceNeeded,
                    appt.AdditionalNotes,
                    appt.Status,
                    appt.IsSpecialAppointment,
                    appt.CreatedAt,
                    appt.ReminderSent
                },
                treatments,
                pricing = new
                {
                    estimatedBaseTotal = estimatedBase,
                    estimatedTotal,
                    estimatedSavings = Math.Max(0, estimatedBase - estimatedTotal)
                },
                assignedDentist = dentist == null
                    ? null
                    : new { dentist.Id, dentist.FirstName, dentist.LastName, dentist.Email },
                treatmentRecord = treatmentRecord == null
                    ? null
                    : new
                    {
                        treatmentRecord.Diagnosis,
                        treatmentRecord.TreatmentPerformed,
                        treatmentRecord.Recommendations,
                        treatmentRecord.MedicationPrescribed,
                        treatmentRecord.UpdatedAt
                    },
                receipt = receiptInfo
            });
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

            var statusMessage = dto.Status switch
            {
                "InProgress" => "You have been checked in. Your visit is now in progress.",
                "Completed" => "Your visit has been completed. Thank you for choosing Oexa Dental Clinic.",
                "Cancelled" => "Your appointment has been cancelled.",
                _ => $"Appointment status changed to {dto.Status}."
            };
            await _email.SendStatusChangedAsync(appt, statusMessage);
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
            var allProblems = await DentalProblemLookup.LoadAllAsync(_db);
            var problems = DentalProblemLookup.FilterByKeys(allProblems, keys);
            if (problems.Count == 0)
                return BadRequest(new { error = "Invalid treatments on appointment. Ask admin to check treatment keys." });

            var categories = problems.Select(p => p.DentistCategoryKey).Distinct().ToList();
            if (categories.Count > 1)
                return BadRequest(new { error = "This booking needs a different dentist per treatment. Use Assign on each treatment row." });

            if (problems.Any(p => p.DentistCategoryKey != dentist.DentistServiceKey))
                return BadRequest(new { error = "This dentist does not handle all selected treatments." });

            await EnsureTreatmentLinesAsync(appt);
            var lines = await _db.AppointmentTreatments.Where(t => t.AppointmentId == id).ToListAsync();
            var start = appt.PreferredDateTime;

            foreach (var line in lines)
            {
                if (!await _scheduling.IsDentistAvailableForLineAsync(
                        dentist.Id, start, line.DurationMinutes, id, line.Id))
                    return BadRequest(new { error = "Dentist is not available at this time." });

                line.AssignedDentistUserId = dentist.Id;
                line.ScheduledStart = start;
            }

            appt.AssignedDentistUserId = dentist.Id;
            await _db.SaveChangesAsync();
            await _email.SendAppointmentAssignedAsync(appt, dentist);
            return Ok(appt);
        }

        [HttpPatch("{id:int}/assign-treatment")]
        public async Task<IActionResult> AssignTreatment(int id, [FromBody] AssignTreatmentDto dto)
        {
            var appt = await _db.Appointments.FindAsync(id);
            if (appt == null) return NotFound();

            if (appt.Status != "Booked")
                return BadRequest(new { error = "Can only assign dentists to booked appointments." });

            var dentist = await _db.Users.FindAsync(dto.DentistUserId);
            if (dentist == null || dentist.Role != "Dentist")
                return BadRequest(new { error = "Invalid dentist." });

            await EnsureTreatmentLinesAsync(appt);
            var lines = await _db.AppointmentTreatments.Where(t => t.AppointmentId == id).ToListAsync();
            var problemKey = dto.ProblemKey.Trim();
            var line = lines.FirstOrDefault(t =>
                string.Equals(t.ProblemKey, problemKey, StringComparison.OrdinalIgnoreCase));
            if (line == null)
                return BadRequest(new { error = "Treatment not found on this appointment." });

            var allProblems = await DentalProblemLookup.LoadAllAsync(_db);
            var problem = DentalProblemLookup.Find(allProblems, line.ProblemKey);
            if (problem == null)
                return BadRequest(new { error = "Invalid treatment." });

            if (problem.DentistCategoryKey != dentist.DentistServiceKey)
                return BadRequest(new { error = "This dentist does not perform this treatment." });

            var start = line.ScheduledStart;
            if (!string.IsNullOrWhiteSpace(dto.PreferredDate) && !string.IsNullOrWhiteSpace(dto.PreferredTime))
            {
                var parsed = ParseDateTime(dto.PreferredDate.Trim(), dto.PreferredTime.Trim());
                if (parsed == null)
                    return BadRequest(new { error = "Invalid date or time for this treatment." });
                if (parsed.Value < DateTime.Now)
                    return BadRequest(new { error = "Cannot schedule in the past." });
                start = parsed.Value;
                line.ScheduledStart = start;
            }

            if (!await _scheduling.IsDentistAvailableForLineAsync(
                    dentist.Id, start, line.DurationMinutes, id, line.Id))
            {
                return BadRequest(new
                {
                    error = "Dentist is busy at this time. Choose another time for this treatment only.",
                    needsReschedule = true,
                    problemKey = line.ProblemKey
                });
            }

            line.AssignedDentistUserId = dentist.Id;
            await SyncAppointmentHeaderAsync(appt, lines);
            await _db.SaveChangesAsync();

            var allAssigned = lines.All(t => t.AssignedDentistUserId != null);
            if (allAssigned)
                await _email.SendAppointmentAssignedAsync(appt, dentist);
            else
                await _email.SendTreatmentLineAssignedAsync(appt, line, dentist, problem.Name);

            return Ok(new
            {
                appointmentId = id,
                line.ProblemKey,
                line.AssignedDentistUserId,
                line.ScheduledStart,
                allTreatmentsAssigned = allAssigned
            });
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

            var previousDateTime = appt.PreferredDateTime;
            appt.PreferredDateTime = preferredDateTime.Value;
            appt.ServiceNeeded = string.Join(",", serviceKeys);
            appt.Status = "Booked";

            var treatmentLines = await _db.AppointmentTreatments.Where(t => t.AppointmentId == id).ToListAsync();
            foreach (var line in treatmentLines)
                line.ScheduledStart = preferredDateTime.Value;

            await _db.SaveChangesAsync();
            await _email.SendAppointmentRescheduledAsync(appt, previousDateTime);
            return Ok(appt);
        }

        private async Task EnsureTreatmentLinesForOpenAppointmentsAsync()
        {
            var open = await _db.Appointments
                .Where(a => a.Status == "Booked")
                .ToListAsync();
            foreach (var appt in open)
                await EnsureTreatmentLinesAsync(appt);
        }

        private async Task EnsureTreatmentLinesAsync(Appointment appt)
        {
            if (await _db.AppointmentTreatments.AnyAsync(t => t.AppointmentId == appt.Id))
                return;

            var keys = AppointmentSchedulingService.ParseServiceKeys(appt.ServiceNeeded);
            var allProblems = await DentalProblemLookup.LoadAllAsync(_db);
            foreach (var key in keys)
            {
                var problem = DentalProblemLookup.Find(allProblems, key);
                var duration = problem?.DurationMinutes > 0 ? problem!.DurationMinutes : 60;
                _db.AppointmentTreatments.Add(new AppointmentTreatment
                {
                    AppointmentId = appt.Id,
                    ProblemKey = problem?.Key ?? key,
                    ScheduledStart = appt.PreferredDateTime,
                    DurationMinutes = duration,
                    AssignedDentistUserId = keys.Count == 1 ? appt.AssignedDentistUserId : null
                });
            }

            await _db.SaveChangesAsync();
        }

        private async Task SyncAppointmentHeaderAsync(Appointment appt, List<AppointmentTreatment> lines)
        {
            if (lines.Count == 0)
                lines = await _db.AppointmentTreatments.Where(t => t.AppointmentId == appt.Id).ToListAsync();

            var assigned = lines.Where(t => t.AssignedDentistUserId != null).Select(t => t.AssignedDentistUserId!.Value).Distinct().ToList();
            if (assigned.Count == 1)
                appt.AssignedDentistUserId = assigned[0];
            else if (assigned.Count == 0)
                appt.AssignedDentistUserId = null;
            else
                appt.AssignedDentistUserId = null;

            if (lines.Count > 0)
            {
                var earliest = lines.Min(t => t.ScheduledStart);
                appt.PreferredDateTime = earliest;
            }
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
