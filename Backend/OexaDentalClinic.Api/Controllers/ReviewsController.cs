using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OexaDentalClinic.Api.Data;
using OexaDentalClinic.Api.DTOs;
using OexaDentalClinic.Api.Models;

namespace OexaDentalClinic.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ReviewsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateReviewDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var appt = await _db.Appointments.FindAsync(dto.AppointmentId);
            if (appt == null) return NotFound(new { error = "Appointment not found." });
            if (appt.Status != "Completed")
                return BadRequest(new { error = "Only completed appointments can be rated." });

            if (await _db.Reviews.AnyAsync(r => r.AppointmentId == dto.AppointmentId))
                return BadRequest(new { error = "Appointment already rated." });

            var review = new Review
            {
                AppointmentId = dto.AppointmentId,
                Rating = dto.Rating,
                Comment = dto.Comment?.Trim()
            };

            _db.Reviews.Add(review);
            await _db.SaveChangesAsync();
            return Ok(review);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _db.Reviews.OrderByDescending(r => r.CreatedAt).ToListAsync());
        }
    }
}
