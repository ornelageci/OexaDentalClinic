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
            var list = keys.ToList();
            var problems = await _db.DentalProblems
                .Where(p => list.Contains(p.Key))
                .ToListAsync();

            if (problems.Count != list.Count)
                throw new InvalidOperationException("One or more treatments are invalid.");

            return problems;
        }

        public int GetTotalDurationMinutes(IEnumerable<DentalProblem> problems)
        {
            return problems.Sum(p => p.DurationMinutes);
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

            var problemMap = await _db.DentalProblems.ToDictionaryAsync(p => p.Key, p => p);
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

            var problemMap = await _db.DentalProblems.ToDictionaryAsync(p => p.Key, p => p);
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
    }
}
