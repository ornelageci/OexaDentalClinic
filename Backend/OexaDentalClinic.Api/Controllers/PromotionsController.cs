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
            return Ok(items);
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActive()
        {
            var today = DateTime.UtcNow.Date;
            var items = await _db.Promotions
                .Where(p => p.IsActive && p.StartDate <= today && p.EndDate >= today)
                .OrderByDescending(p => p.DiscountPercent)
                .ToListAsync();
            return Ok(items);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePromotionDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (!DateTime.TryParse(dto.StartDate, out var start) || !DateTime.TryParse(dto.EndDate, out var end))
                return BadRequest(new { error = "Invalid date format." });

            var promo = new Promotion
            {
                Title = dto.Title.Trim(),
                Description = dto.Description?.Trim(),
                DiscountPercent = dto.DiscountPercent,
                StartDate = start,
                EndDate = end,
                TargetAudience = dto.TargetAudience?.Trim(),
                ProblemKey = dto.ProblemKey?.Trim(),
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
