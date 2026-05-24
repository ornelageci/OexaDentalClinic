using Microsoft.EntityFrameworkCore;
using OexaDentalClinic.Api.Data;
using OexaDentalClinic.Api.Models;

namespace OexaDentalClinic.Api.Services
{
    public class AppointmentSchedulingService
    {
        private readonly AppDbContext _db;
        private const int SlotStepMinutes = 15;

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

        public static int LineDurationMinutes(AppointmentTreatment line) =>
            line.DurationMinutes > 0 ? line.DurationMinutes : 60;

        public static (DateTime start, DateTime end) GetBookingWindow(Appointment appt, IReadOnlyList<AppointmentTreatment> lines)
        {
            var start = appt.PreferredDateTime;
            var total = lines.Sum(LineDurationMinutes);
            if (total <= 0) total = 60;
            return (start, start.AddMinutes(total));
        }

        public static List<DentalProblem> OrderProblemsShorterFirst(IEnumerable<DentalProblem> problems) =>
            problems.OrderBy(p => p.DurationMinutes > 0 ? p.DurationMinutes : 60).ToList();

        public static List<AppointmentTreatment> OrderLinesShorterFirst(IEnumerable<AppointmentTreatment> lines) =>
            lines.OrderBy(LineDurationMinutes).ThenBy(l => l.Id).ToList();

        public async Task<bool> IsSlotAvailableAsync(IEnumerable<string> serviceKeys, DateTime start, int? excludeAppointmentId = null)
        {
            var problems = OrderProblemsShorterFirst(await GetProblemsForKeysAsync(serviceKeys));
            var appointments = await _db.Appointments
                .Where(a => a.Status != "Cancelled" && a.Id != excludeAppointmentId)
                .Where(a => a.PreferredDateTime.Date == start.Date)
                .ToListAsync();

            var problemMap = await DentalProblemLookup.ByKeyAsync(_db);
            var dentists = await _db.Users.Where(u => u.Role == "Dentist").ToListAsync();
            var treatmentLines = await LoadDayTreatmentLinesAsync(start.Date, excludeAppointmentId);

            return await IsSequentialSlotAvailableAsync(problems, start, appointments, problemMap, dentists, treatmentLines);
        }

        public async Task<List<object>> GetTimeSlotsAsync(string dateStr, IEnumerable<string> serviceKeys)
        {
            if (!DateTime.TryParseExact(dateStr, new[] { "dd.MM.yyyy", "d.M.yyyy", "yyyy-MM-dd" },
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var day))
                throw new ArgumentException("Invalid date.");

            var problems = OrderProblemsShorterFirst(await GetProblemsForKeysAsync(serviceKeys));
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
            var treatmentLines = await LoadDayTreatmentLinesAsync(dayStart, null);

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

                    var available = await IsSequentialSlotAvailableAsync(
                        problems, start, appointments, problemMap, dentists, treatmentLines);

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

        public async Task<List<object>> GetDentistTimeSlotsAsync(
            int dentistUserId,
            string dateStr,
            int durationMinutes,
            int appointmentId,
            int? excludeTreatmentLineId = null)
        {
            if (!DateTime.TryParseExact(dateStr, new[] { "dd.MM.yyyy", "d.M.yyyy", "yyyy-MM-dd" },
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var day))
                throw new ArgumentException("Invalid date.");

            var duration = durationMinutes > 0 ? durationMinutes : 60;
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

            var siblingLines = await _db.AppointmentTreatments
                .Where(t => t.AppointmentId == appointmentId)
                .ToListAsync();

            var appt = await _db.Appointments.FindAsync(appointmentId);
            if (appt == null)
                throw new InvalidOperationException("Appointment not found.");

            var (windowStart, windowEnd) = GetBookingWindow(appt, siblingLines);

            var slots = new List<object>();
            var lastStart = windowEnd.AddMinutes(-duration);
            if (lastStart < windowStart)
                lastStart = windowStart;

            for (var t = windowStart; t <= lastStart; t = t.AddMinutes(SlotStepMinutes))
            {
                if (t.Date != dayStart)
                    continue;

                var end = t.AddMinutes(duration);
                if (end > dayStart.AddHours(closeHour))
                    continue;
                if (dayStart == DateTime.Today && t <= DateTime.Now)
                    continue;

                var dentistFree = await IsDentistAvailableForLineAsync(
                    dentistUserId, t, duration, appointmentId, excludeTreatmentLineId);
                var patientFree = IsPatientSlotFree(t, duration, siblingLines, excludeTreatmentLineId ?? 0);

                slots.Add(new
                {
                    time = t.ToString("HH:mm"),
                    label = FormatSlotLabel(t, end),
                    available = dentistFree && patientFree,
                    endTime = end.ToString("HH:mm")
                });
            }

            return slots;
        }

        public async Task<DateTime?> FindEarliestAvailableStartAsync(
            int dentistUserId,
            AppointmentTreatment line,
            IReadOnlyList<AppointmentTreatment> siblingLines,
            DateTime windowStart,
            DateTime windowEnd,
            int appointmentId)
        {
            var duration = LineDurationMinutes(line);
            var lastStart = windowEnd.AddMinutes(-duration);
            if (lastStart < windowStart)
                return null;

            for (var t = windowStart; t <= lastStart; t = t.AddMinutes(SlotStepMinutes))
            {
                if (!await IsDentistAvailableForLineAsync(dentistUserId, t, duration, appointmentId, line.Id))
                    continue;
                if (!IsPatientSlotFree(t, duration, siblingLines, line.Id))
                    continue;
                return t;
            }

            return null;
        }

        public void ApplySequentialSchedule(List<AppointmentTreatment> lines, DateTime windowStart)
        {
            var ordered = OrderLinesShorterFirst(lines);
            var current = windowStart;
            foreach (var line in ordered)
            {
                line.ScheduledStart = current;
                current = current.AddMinutes(LineDurationMinutes(line));
            }
        }

        public void RecalculateUnassignedLineStarts(List<AppointmentTreatment> lines, DateTime windowStart)
        {
            var ordered = OrderLinesShorterFirst(lines);
            var current = windowStart;

            foreach (var line in ordered)
            {
                if (line.AssignedDentistUserId != null)
                {
                    if (line.ScheduledStart < current)
                        line.ScheduledStart = current;
                    current = line.ScheduledStart.AddMinutes(LineDurationMinutes(line));
                }
                else
                {
                    line.ScheduledStart = current;
                    current = current.AddMinutes(LineDurationMinutes(line));
                }
            }
        }

        public async Task CreateTreatmentLinesAsync(int appointmentId, IReadOnlyList<DentalProblem> problems, DateTime windowStart)
        {
            var ordered = OrderProblemsShorterFirst(problems);
            var current = windowStart;

            foreach (var problem in ordered)
            {
                var duration = problem.DurationMinutes > 0 ? problem.DurationMinutes : 60;
                _db.AppointmentTreatments.Add(new AppointmentTreatment
                {
                    AppointmentId = appointmentId,
                    ProblemKey = problem.Key,
                    ScheduledStart = current,
                    DurationMinutes = duration,
                    AssignedDentistUserId = null
                });
                current = current.AddMinutes(duration);
            }

            await _db.SaveChangesAsync();
        }

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
                if (line.AppointmentId == appointmentId && excludeTreatmentLineId == line.Id) continue;
                var lineEnd = line.ScheduledStart.AddMinutes(LineDurationMinutes(line));
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

        private async Task<bool> IsSequentialSlotAvailableAsync(
            List<DentalProblem> orderedProblems,
            DateTime windowStart,
            List<Appointment> dayAppointments,
            Dictionary<string, DentalProblem> problemMap,
            List<User> allDentists,
            List<AppointmentTreatment> dayTreatmentLines)
        {
            var current = windowStart;
            foreach (var problem in orderedProblems)
            {
                var duration = problem.DurationMinutes > 0 ? problem.DurationMinutes : 60;
                var end = current.AddMinutes(duration);

                if (!IsCategoryAvailableAtInterval(
                        problem.DentistCategoryKey, current, end, dayAppointments, problemMap, allDentists, dayTreatmentLines))
                    return false;

                current = end;
            }

            return true;
        }

        private static bool IsCategoryAvailableAtInterval(
            string category,
            DateTime start,
            DateTime end,
            List<Appointment> dayAppointments,
            Dictionary<string, DentalProblem> problemMap,
            List<User> allDentists,
            List<AppointmentTreatment> dayTreatmentLines)
        {
            var dentistsInCategory = allDentists.Where(d => d.DentistServiceKey == category).ToList();
            if (dentistsInCategory.Count == 0)
                return false;

            var busyDentistIds = new HashSet<int>();

            foreach (var line in dayTreatmentLines)
            {
                if (!problemMap.TryGetValue(line.ProblemKey, out var prob)) continue;
                if (prob.DentistCategoryKey != category) continue;

                var lineEnd = line.ScheduledStart.AddMinutes(LineDurationMinutes(line));
                if (!IntervalsOverlap(start, end, line.ScheduledStart, lineEnd)) continue;

                if (line.AssignedDentistUserId.HasValue)
                    busyDentistIds.Add(line.AssignedDentistUserId.Value);
            }

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

                var hasLines = dayTreatmentLines.Any(t => t.AppointmentId == appt.Id);
                if (hasLines)
                    continue;

                var apptDuration = apptKeys
                    .Where(k => problemMap.ContainsKey(k))
                    .Sum(k => problemMap[k].DurationMinutes);

                var apptStart = appt.PreferredDateTime;
                var apptEnd = apptStart.AddMinutes(apptDuration > 0 ? apptDuration : 60);

                if (!IntervalsOverlap(start, end, apptStart, apptEnd))
                    continue;

                if (appt.AssignedDentistUserId.HasValue &&
                    dentistsInCategory.Any(d => d.Id == appt.AssignedDentistUserId.Value))
                    busyDentistIds.Add(appt.AssignedDentistUserId.Value);
            }

            var unassignedOverlapping = dayTreatmentLines.Count(line =>
            {
                if (!problemMap.TryGetValue(line.ProblemKey, out var prob)) return false;
                if (prob.DentistCategoryKey != category) return false;
                if (line.AssignedDentistUserId.HasValue) return false;
                var lineEnd = line.ScheduledStart.AddMinutes(LineDurationMinutes(line));
                return IntervalsOverlap(start, end, line.ScheduledStart, lineEnd);
            });

            unassignedOverlapping += dayAppointments.Count(appt =>
            {
                if (dayTreatmentLines.Any(t => t.AppointmentId == appt.Id)) return false;
                var apptKeys = ParseServiceKeys(appt.ServiceNeeded);
                if (!apptKeys.Any(k => problemMap.TryGetValue(k, out var p) && p.DentistCategoryKey == category))
                    return false;
                if (appt.AssignedDentistUserId.HasValue) return false;
                var dur = apptKeys.Where(problemMap.ContainsKey).Sum(k => problemMap[k].DurationMinutes);
                var apptEnd = appt.PreferredDateTime.AddMinutes(dur > 0 ? dur : 60);
                return IntervalsOverlap(start, end, appt.PreferredDateTime, apptEnd);
            });

            var freeCount = dentistsInCategory.Count(d => !busyDentistIds.Contains(d.Id));
            return freeCount > unassignedOverlapping;
        }

        private async Task<List<AppointmentTreatment>> LoadDayTreatmentLinesAsync(DateTime day, int? excludeAppointmentId)
        {
            var lines = await _db.AppointmentTreatments
                .Where(t => t.ScheduledStart.Date == day)
                .ToListAsync();

            if (!excludeAppointmentId.HasValue)
                return lines;

            var cancelled = await _db.Appointments
                .Where(a => a.Id == excludeAppointmentId && a.Status == "Cancelled")
                .AnyAsync();

            if (cancelled)
                return lines;

            return lines.Where(t => t.AppointmentId != excludeAppointmentId.Value).ToList();
        }

        public static bool IsPatientSlotFree(
            DateTime start,
            int durationMinutes,
            IReadOnlyList<AppointmentTreatment> siblingLines,
            int excludeLineId)
        {
            var end = start.AddMinutes(durationMinutes > 0 ? durationMinutes : 60);
            foreach (var sibling in siblingLines)
            {
                if (sibling.Id == excludeLineId) continue;
                var siblingEnd = sibling.ScheduledStart.AddMinutes(LineDurationMinutes(sibling));
                if (IntervalsOverlap(start, end, sibling.ScheduledStart, siblingEnd))
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
    }
}
