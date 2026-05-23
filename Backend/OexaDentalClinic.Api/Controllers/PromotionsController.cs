using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OexaDentalClinic.Api.Data;
using OexaDentalClinic.Api.DTOs;
using OexaDentalClinic.Api.Models;

namespace OexaDentalClinic.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PromotionsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public PromotionsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _db.Promotions.OrderByDescending(p => p.StartDate).ToListAsync();
            var problems = await _db.DentalProblems.ToDictionaryAsync(p => p.Key, p => p.Name);

            return Ok(items.Select(p => new
            {
                p.Id,
                p.Title,
                p.Description,
                p.DiscountPercent,
                p.StartDate,
                p.EndDate,
                p.IsActive,
                p.TargetAudience,
                p.ProblemKey,
                TreatmentName = p.ProblemKey != null && problems.ContainsKey(p.ProblemKey)
                    ? problems[p.ProblemKey]
                    : null
            }));
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActive()
        {
            var today = DateTime.Today;
            var items = await _db.Promotions
                .Where(p => p.IsActive && p.StartDate.Date <= today && p.EndDate.Date >= today)
                .OrderByDescending(p => p.DiscountPercent)
                .ToListAsync();

            var problems = await _db.DentalProblems.ToDictionaryAsync(p => p.Key, p => p);

            var result = items.Select(p =>
            {
                decimal? basePrice = null;
                decimal? priceAfter = null;
                string? treatmentName = null;

                if (!string.IsNullOrEmpty(p.ProblemKey) && problems.TryGetValue(p.ProblemKey, out var prob))
                {
                    treatmentName = prob.Name;
                    basePrice = prob.BasePrice;
                    priceAfter = Math.Round(prob.BasePrice * (100 - p.DiscountPercent) / 100m, 2);
                }

                return new
                {
                    p.Id,
                    p.Title,
                    p.Description,
                    p.DiscountPercent,
                    p.StartDate,
                    p.EndDate,
                    p.ProblemKey,
                    TreatmentName = treatmentName,
                    BasePrice = basePrice,
                    PriceAfterDiscount = priceAfter
                };
            });

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePromotionDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (!DateTime.TryParse(dto.StartDate, out var start) || !DateTime.TryParse(dto.EndDate, out var end))
                return BadRequest(new { error = "Invalid date format." });

            if (string.IsNullOrWhiteSpace(dto.ProblemKey))
                return BadRequest(new { error = "Select a treatment to apply this discount to." });

            var problemKey = dto.ProblemKey.Trim().ToLowerInvariant();
            var problem = await _db.DentalProblems.FirstOrDefaultAsync(p => p.Key == problemKey);
            if (problem == null)
                return BadRequest(new { error = "Selected treatment does not exist." });

            if (dto.DiscountPercent < 1 || dto.DiscountPercent > 90)
                return BadRequest(new { error = "Discount must be between 1 and 90 percent." });

            var promo = new Promotion
            {
                Title = dto.Title.Trim(),
                Description = dto.Description?.Trim(),
                DiscountPercent = dto.DiscountPercent,
                StartDate = start,
                EndDate = end,
                TargetAudience = dto.TargetAudience?.Trim(),
                ProblemKey = problemKey,
                IsActive = true
            };

            _db.Promotions.Add(promo);
            await _db.SaveChangesAsync();
            return Ok(promo);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var promo = await _db.Promotions.FindAsync(id);
            if (promo == null) return NotFound();
            _db.Promotions.Remove(promo);
            await _db.SaveChangesAsync();
            return Ok();
        }
    }
}
