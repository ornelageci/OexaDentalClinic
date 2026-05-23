using Microsoft.EntityFrameworkCore;
using OexaDentalClinic.Api.Data;
using OexaDentalClinic.Api.Models;

namespace OexaDentalClinic.Api.Services
{
    public class AppointmentSchedulingService
    {
        private readonly AppDbContext _db;

        public AppointmentSchedulingService(AppDbContext db)
        {
            _db = db;
        }

        public static List<string> ParseServiceKeys(string serviceNeeded)
        {
            return serviceNeeded
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public async Task<List<DentalProblem>> GetProblemsForKeysAsync(IEnumerable<string> keys)
        {
            var keyList = keys.Select(k => k.Trim()).Where(k => k.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var all = await DentalProblemLookup.LoadAllAsync(_db);
            var problems = DentalProblemLookup.FilterByKeys(all, keyList);

            if (problems.Count != keyList.Count)
                throw new InvalidOperationException("One or more treatments are invalid.");

            return problems;
        }

        public int GetTotalDurationMinutes(IEnumerable<DentalProblem> problems)
        {
            var total = problems.Sum(p => p.DurationMinutes > 0 ? p.DurationMinutes : 60);
            return total > 0 ? total : 60;
        }

        public async Task<bool> IsSlotAvailableAsync(IEnumerable<string> serviceKeys, DateTime start, int? excludeAppointmentId = null)
        {
            var problems = await GetProblemsForKeysAsync(serviceKeys);
            var duration = GetTotalDurationMinutes(problems);
            var end = start.AddMinutes(duration);

            var appointments = await _db.Appointments
                .Where(a => a.Status != "Cancelled" && a.Id != excludeAppointmentId)
                .Where(a => a.PreferredDateTime.Date == start.Date)
                .ToListAsync();

            var problemMap = await DentalProblemLookup.ByKeyAsync(_db);
            var dentists = await _db.Users.Where(u => u.Role == "Dentist").ToListAsync();

            return CheckCapacity(problems, start, end, appointments, problemMap, dentists);
        }

        public async Task<List<object>> GetTimeSlotsAsync(string dateStr, IEnumerable<string> serviceKeys)
        {
            if (!DateTime.TryParseExact(dateStr, new[] { "dd.MM.yyyy", "d.M.yyyy", "yyyy-MM-dd" },
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var day))
                throw new ArgumentException("Invalid date.");

            var problems = await GetProblemsForKeysAsync(serviceKeys);
            var duration = GetTotalDurationMinutes(problems);
            var dayStart = day.Date;

            if (dayStart < DateTime.Today)
                return new List<object>();

            var (openHour, closeHour) = dayStart.DayOfWeek == DayOfWeek.Saturday
                ? (9, 14)
                : dayStart.DayOfWeek == DayOfWeek.Sunday
                    ? (-1, -1)
                    : (9, 18);

            if (openHour < 0)
                return new List<object>();

            var appointments = await _db.Appointments
                .Where(a => a.Status != "Cancelled" && a.PreferredDateTime.Date == dayStart)
                .ToListAsync();

            var problemMap = await DentalProblemLookup.ByKeyAsync(_db);
            var dentists = await _db.Users.Where(u => u.Role == "Dentist").ToListAsync();

            var slots = new List<object>();
            for (var hour = openHour; hour < closeHour; hour++)
            {
                for (var minute = 0; minute < 60; minute += 30)
                {
                    var start = dayStart.AddHours(hour).AddMinutes(minute);
                    var end = start.AddMinutes(duration);
                    if (end > dayStart.AddHours(closeHour))
                        continue;
                    if (dayStart == DateTime.Today && start <= DateTime.Now)
                        continue;

                    var available = CheckCapacity(problems, start, end, appointments, problemMap, dentists);

                    slots.Add(new
                    {
                        time = start.ToString("HH:mm"),
                        label = FormatSlotLabel(start, end),
                        available,
                        endTime = end.ToString("HH:mm")
                    });
                }
            }

            return slots;
        }

        private static bool CheckCapacity(
            List<DentalProblem> selectedProblems,
            DateTime start,
            DateTime end,
            List<Appointment> dayAppointments,
            Dictionary<string, DentalProblem> problemMap,
            List<User> allDentists)
        {
            var categories = selectedProblems.Select(p => p.DentistCategoryKey).Distinct();

            foreach (var category in categories)
            {
                var dentistsInCategory = allDentists.Where(d => d.DentistServiceKey == category).ToList();
                if (dentistsInCategory.Count == 0)
                    return false;

                var busyDentistIds = new HashSet<int>();
                var unassignedCount = 0;

                foreach (var appt in dayAppointments)
                {
                    var apptKeys = ParseServiceKeys(appt.ServiceNeeded);
                    var apptCategories = apptKeys
                        .Where(k => problemMap.ContainsKey(k))
                        .Select(k => problemMap[k].DentistCategoryKey)
                        .Distinct()
                        .ToList();

                    if (!apptCategories.Contains(category))
                        continue;

                    var apptDuration = apptKeys
                        .Where(k => problemMap.ContainsKey(k))
                        .Sum(k => problemMap[k].DurationMinutes);

                    var apptStart = appt.PreferredDateTime;
                    var apptEnd = apptStart.AddMinutes(apptDuration);

                    if (!IntervalsOverlap(start, end, apptStart, apptEnd))
                        continue;

                    if (appt.AssignedDentistUserId.HasValue)
                    {
                        if (dentistsInCategory.Any(d => d.Id == appt.AssignedDentistUserId.Value))
                            busyDentistIds.Add(appt.AssignedDentistUserId.Value);
                    }
                    else
                    {
                        unassignedCount++;
                    }
                }

                var freeCount = dentistsInCategory.Count(d => !busyDentistIds.Contains(d.Id));
                if (freeCount <= unassignedCount)
                    return false;
            }

            return true;
        }

        private static bool IntervalsOverlap(DateTime aStart, DateTime aEnd, DateTime bStart, DateTime bEnd)
        {
            return aStart < bEnd && bStart < aEnd;
        }

        private static string FormatSlotLabel(DateTime start, DateTime end)
        {
            return $"{start:hh:mm tt} – {end:hh:mm tt}";
        }

        public async Task CreateTreatmentLinesAsync(int appointmentId, IReadOnlyList<DentalProblem> problems, DateTime start)
        {
            foreach (var problem in problems)
            {
                _db.AppointmentTreatments.Add(new AppointmentTreatment
                {
                    AppointmentId = appointmentId,
                    ProblemKey = problem.Key,
                    ScheduledStart = start,
                    DurationMinutes = problem.DurationMinutes > 0 ? problem.DurationMinutes : 60,
                    AssignedDentistUserId = null
                });
            }

            await _db.SaveChangesAsync();
        }

        /// <summary>True if dentist has no overlapping assigned treatment at this time.</summary>
        public async Task<bool> IsDentistAvailableForLineAsync(
            int dentistUserId,
            DateTime start,
            int durationMinutes,
            int appointmentId,
            int? excludeTreatmentLineId = null)
        {
            var end = start.AddMinutes(durationMinutes > 0 ? durationMinutes : 60);
            var day = start.Date;

            var lines = await _db.AppointmentTreatments
                .Where(t => t.AssignedDentistUserId == dentistUserId)
                .Where(t => t.ScheduledStart.Date == day)
                .Where(t => excludeTreatmentLineId == null || t.Id != excludeTreatmentLineId.Value)
                .ToListAsync();

            var apptIds = lines.Select(t => t.AppointmentId).Distinct().ToList();
            var cancelled = await _db.Appointments
                .Where(a => apptIds.Contains(a.Id) && a.Status == "Cancelled")
                .Select(a => a.Id)
                .ToListAsync();

            foreach (var line in lines)
            {
                if (cancelled.Contains(line.AppointmentId)) continue;
                var lineEnd = line.ScheduledStart.AddMinutes(line.DurationMinutes > 0 ? line.DurationMinutes : 60);
                if (IntervalsOverlap(start, end, line.ScheduledStart, lineEnd))
                    return false;
            }

            var apptsWithLines = await _db.AppointmentTreatments
                .Select(t => t.AppointmentId)
                .Distinct()
                .ToListAsync();

            var legacyAppts = await _db.Appointments
                .Where(a => a.AssignedDentistUserId == dentistUserId && a.Status != "Cancelled")
                .Where(a => a.PreferredDateTime.Date == day && a.Id != appointmentId)
                .Where(a => !apptsWithLines.Contains(a.Id))
                .ToListAsync();

            var problemMap = await DentalProblemLookup.ByKeyAsync(_db);
            foreach (var appt in legacyAppts)
            {
                var keys = ParseServiceKeys(appt.ServiceNeeded);
                var dur = keys.Where(problemMap.ContainsKey).Sum(k => problemMap[k].DurationMinutes);
                if (dur <= 0) dur = 60;
                var apptEnd = appt.PreferredDateTime.AddMinutes(dur);
                if (IntervalsOverlap(start, end, appt.PreferredDateTime, apptEnd))
                    return false;
            }

            return true;
        }
    }
}
