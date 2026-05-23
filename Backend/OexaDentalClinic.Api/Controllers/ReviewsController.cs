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
    public class ReviewsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IEmailService _email;

        public ReviewsController(AppDbContext db, IEmailService email)
        {
            _db = db;
            _email = email;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateReviewDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var appt = await _db.Appointments.FindAsync(dto.AppointmentId);
            if (appt == null) return NotFound(new { error = "Appointment not found." });
            if (appt.Status != "Completed")
                return BadRequest(new { error = "Only completed appointments can be rated." });

            if (dto.Rating < 1 || dto.Rating > 5)
                return BadRequest(new { error = "Rating must be between 1 and 5." });

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
            await _email.SendReviewSubmittedAsync(appt, review);
            return Ok(review);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _db.Reviews.OrderByDescending(r => r.CreatedAt).ToListAsync());
        }
    }
}
