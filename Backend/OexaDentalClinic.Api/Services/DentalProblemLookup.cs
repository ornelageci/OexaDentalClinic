using Microsoft.EntityFrameworkCore;
using OexaDentalClinic.Api.Data;
using OexaDentalClinic.Api.Models;

namespace OexaDentalClinic.Api.Services
{
    /// <summary>
    /// In-memory dental problem lookups. Avoids EF queries with StringComparison / OrdinalIgnoreCase
    /// (MySQL provider throws on COLLATE / @keys translation).
    /// </summary>
    public static class DentalProblemLookup
    {
        public static async Task<List<DentalProblem>> LoadAllAsync(AppDbContext db, CancellationToken ct = default) =>
            await db.DentalProblems.AsNoTracking().ToListAsync(ct);

        public static async Task<Dictionary<string, DentalProblem>> ByKeyAsync(AppDbContext db, CancellationToken ct = default)
        {
            var list = await LoadAllAsync(db, ct);
            return list.ToDictionary(p => p.Key, p => p, StringComparer.OrdinalIgnoreCase);
        }

        public static async Task<Dictionary<string, string>> NameByKeyAsync(AppDbContext db, CancellationToken ct = default)
        {
            var list = await LoadAllAsync(db, ct);
            return list.ToDictionary(p => p.Key, p => p.Name, StringComparer.OrdinalIgnoreCase);
        }

        public static DentalProblem? Find(IEnumerable<DentalProblem> problems, string key) =>
            problems.FirstOrDefault(p => string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase));

        public static List<DentalProblem> FilterByKeys(IEnumerable<DentalProblem> problems, IEnumerable<string> keys)
        {
            var keySet = keys.Select(k => k.Trim()).Where(k => k.Length > 0).ToHashSet(StringComparer.OrdinalIgnoreCase);
            return problems.Where(p => keySet.Contains(p.Key)).ToList();
        }
    }
}
