using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OexaDentalClinic.Api.Data;
using OexaDentalClinic.Api.DTOs;
using OexaDentalClinic.Api.Models;
using OexaDentalClinic.Api.Services;
using System.Text.RegularExpressions;

namespace OexaDentalClinic.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProblemsController : ControllerBase
    {
        private readonly AppDbContext _db;

        private static readonly string[] ValidCategories =
        {
            "general", "orthodontics", "cosmetic", "pediatric", "oral-surgery"
        };

        public ProblemsController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>Public list for booking — includes active promotion prices.</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var today = DateTime.Today;
            var problems = await _db.DentalProblems.OrderBy(p => p.Name).ToListAsync();
            var promos = await _db.Promotions
                .Where(p => p.IsActive && p.ProblemKey != null && p.StartDate.Date <= today && p.EndDate.Date >= today)
                .ToListAsync();

            var result = problems.Select(p =>
            {
                var promo = promos.FirstOrDefault(x => x.ProblemKey == p.Key);
                var discounted = promo != null
                    ? Math.Round(p.BasePrice * (100 - promo.DiscountPercent) / 100m, 2)
                    : (decimal?)null;

                return new
                {
                    p.Key,
                    p.Name,
                    p.Description,
                    p.BasePrice,
                    p.DurationMinutes,
                    p.DentistCategoryKey,
                    HasPromotion = promo != null,
                    PromotionTitle = promo?.Title,
                    DiscountPercent = promo?.DiscountPercent,
                    PriceAfterDiscount = discounted,
                    DisplayPrice = discounted ?? p.BasePrice
                };
            });

            return Ok(result);
        }

        /// <summary>Catalog for staff (marketer promotions dropdown).</summary>
        [HttpGet("catalog")]
        public async Task<IActionResult> GetCatalog()
        {
            var items = await _db.DentalProblems
                .OrderBy(p => p.Name)
                .Select(p => new
                {
                    p.Id,
                    p.Key,
                    p.Name,
                    p.BasePrice,
                    p.DurationMinutes,
                    p.DentistCategoryKey
                })
                .ToListAsync();

            return Ok(items);
        }

        /// <summary>Full list for admin treatment management.</summary>
        [HttpGet("manage")]
        public async Task<IActionResult> GetForManage()
        {
            var items = await _db.DentalProblems.OrderBy(p => p.Name).ToListAsync();
            return Ok(items);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateDentalProblemDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var key = NormalizeKey(dto.Key);
            if (string.IsNullOrEmpty(key))
                return BadRequest(new { error = "Treatment key is required (e.g. teeth-whitening)." });

            if (!ValidCategories.Contains(dto.DentistCategoryKey.Trim().ToLowerInvariant()))
                return BadRequest(new { error = "Invalid specialist category.", valid = ValidCategories });

            if (await _db.DentalProblems.AnyAsync(p => p.Key == key))
                return BadRequest(new { error = "A treatment with this key already exists." });

            var problem = new DentalProblem
            {
                Key = key,
                Name = dto.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                BasePrice = dto.BasePrice,
                DurationMinutes = dto.DurationMinutes > 0 ? dto.DurationMinutes : 60,
                DentistCategoryKey = dto.DentistCategoryKey.Trim().ToLowerInvariant()
            };

            _db.DentalProblems.Add(problem);
            await _db.SaveChangesAsync();
            return Ok(problem);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateDentalProblemDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var problem = await _db.DentalProblems.FindAsync(id);
            if (problem == null) return NotFound();

            if (!ValidCategories.Contains(dto.DentistCategoryKey.Trim().ToLowerInvariant()))
                return BadRequest(new { error = "Invalid specialist category.", valid = ValidCategories });

            problem.Name = dto.Name.Trim();
            problem.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
            problem.BasePrice = dto.BasePrice;
            problem.DurationMinutes = dto.DurationMinutes > 0 ? dto.DurationMinutes : 60;
            problem.DentistCategoryKey = dto.DentistCategoryKey.Trim().ToLowerInvariant();

            await _db.SaveChangesAsync();
            return Ok(problem);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var problem = await _db.DentalProblems.FindAsync(id);
            if (problem == null) return NotFound();

            var appointments = await _db.Appointments
                .Where(a => a.Status != "Cancelled")
                .Select(a => a.ServiceNeeded)
                .ToListAsync();

            var inUse = appointments.Any(s =>
                AppointmentSchedulingService.ParseServiceKeys(s)
                    .Any(k => string.Equals(k, problem.Key, StringComparison.OrdinalIgnoreCase)));

            if (inUse)
                return BadRequest(new { error = "Cannot delete: this treatment is used by existing appointments." });

            var promos = await _db.Promotions.Where(p => p.ProblemKey == problem.Key).ToListAsync();
            foreach (var promo in promos)
                promo.ProblemKey = null;

            _db.DentalProblems.Remove(problem);
            await _db.SaveChangesAsync();
            return Ok();
        }

        private static string NormalizeKey(string key)
        {
            var normalized = Regex.Replace(key.Trim().ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
            return normalized;
        }
    }
}
