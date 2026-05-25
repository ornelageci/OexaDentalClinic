using Microsoft.EntityFrameworkCore;
using OexaDentalClinic.Api.Data;
using OexaDentalClinic.Api.Models;

namespace OexaDentalClinic.Api.Services
{
    public class ReceiptSyncService
    {
        private readonly AppDbContext _db;
        private readonly AppointmentSchedulingService _scheduling;

        public ReceiptSyncService(AppDbContext db, AppointmentSchedulingService scheduling)
        {
            _db = db;
            _scheduling = scheduling;
        }

        public async Task EnsureAndSyncTreatmentLinesAsync(Receipt receipt, Appointment appointment)
        {
            await _scheduling.EnsureTreatmentLinesAsync(appointment);
            await SyncReceiptTreatmentsAsync(receipt, appointment.Id);
            await BackfillMedicationDentistsAsync(appointment.Id, receipt.Id);
        }

        /// <summary>Assign dentist to legacy medications missing SubmittedByDentistUserId.</summary>
        public async Task BackfillMedicationDentistsAsync(int appointmentId, int receiptId)
        {
            var unassigned = await _db.ReceiptMedications
                .Where(m => m.ReceiptId == receiptId && m.SubmittedByDentistUserId == null)
                .OrderBy(m => m.Id)
                .ToListAsync();
            if (unassigned.Count == 0) return;

            var dentistIds = await _db.AppointmentTreatments
                .Where(t => t.AppointmentId == appointmentId && t.AssignedDentistUserId != null)
                .Select(t => t.AssignedDentistUserId!.Value)
                .Distinct()
                .OrderBy(id => id)
                .ToListAsync();

            if (dentistIds.Count == 0)
            {
                var appt = await _db.Appointments.AsNoTracking()
                    .FirstOrDefaultAsync(a => a.Id == appointmentId);
                if (appt?.AssignedDentistUserId != null)
                    dentistIds.Add(appt.AssignedDentistUserId.Value);
            }

            if (dentistIds.Count == 0) return;

            if (dentistIds.Count == 1)
            {
                foreach (var med in unassigned)
                    med.SubmittedByDentistUserId = dentistIds[0];
            }
            else if (dentistIds.Count == unassigned.Count)
            {
                for (var i = 0; i < unassigned.Count; i++)
                    unassigned[i].SubmittedByDentistUserId = dentistIds[i];
            }

            await _db.SaveChangesAsync();
        }

        public async Task SyncReceiptTreatmentsAsync(Receipt receipt, int appointmentId)
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
    }
}
