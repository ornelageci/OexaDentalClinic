using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OexaDentalClinic.Api.Data;

namespace OexaDentalClinic.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProblemsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ProblemsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var today = DateTime.UtcNow.Date;
            var problems = await _db.DentalProblems.OrderBy(p => p.Name).ToListAsync();
            var promos = await _db.Promotions
                .Where(p => p.IsActive && p.StartDate <= today && p.EndDate >= today && p.ProblemKey != null)
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
                    p.DentistCategoryKey,
                    HasPromotion = promo != null,
                    PromotionTitle = promo?.Title,
                    DiscountPercent = promo?.DiscountPercent,
                    PriceAfterDiscount = discounted
                };
            });

            return Ok(result);
        }
    }
}
