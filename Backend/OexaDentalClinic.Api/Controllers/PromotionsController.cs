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
            var all = await _db.Promotions
                .Where(p => p.IsActive && p.ProblemKey != null)
                .OrderByDescending(p => p.DiscountPercent)
                .ToListAsync();

            var items = all.Where(p => PromotionHelper.IsActiveOnDate(p)).ToList();
            var problems = await _db.DentalProblems.ToListAsync();

            var result = items.Select(p =>
            {
                var prob = problems.FirstOrDefault(x => PromotionHelper.KeysMatch(p.ProblemKey, x.Key));
                decimal? basePrice = null;
                decimal? priceAfter = null;
                string? treatmentName = null;

                if (prob != null)
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
                    PriceAfterDiscount = priceAfter,
                    IsCurrentlyActive = true
                };
            });

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePromotionDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (string.IsNullOrWhiteSpace(dto.StartDate) || string.IsNullOrWhiteSpace(dto.EndDate))
                return BadRequest(new { error = "Start date and end date are required." });

            if (!DateTime.TryParse(dto.StartDate, out var start) || !DateTime.TryParse(dto.EndDate, out var end))
                return BadRequest(new { error = "Invalid date format." });

            start = PromotionHelper.NormalizeDate(start);
            end = PromotionHelper.NormalizeDate(end);

            if (end < start)
                return BadRequest(new { error = "End date must be on or after start date." });

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
