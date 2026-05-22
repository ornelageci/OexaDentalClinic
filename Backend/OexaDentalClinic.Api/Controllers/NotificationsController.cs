using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OexaDentalClinic.Api.Data;

namespace OexaDentalClinic.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public NotificationsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("upcoming")]
        public async Task<IActionResult> GetUpcoming([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new { error = "Email is required." });

            var now = DateTime.Now;
            var items = await _db.Appointments
                .Where(a => a.Email.ToLower() == email.Trim().ToLower()
                    && a.Status == "Booked"
                    && a.PreferredDateTime > now)
                .OrderBy(a => a.PreferredDateTime)
                .Select(a => new
                {
                    a.Id,
                    a.PreferredDateTime,
                    a.ServiceNeeded,
                    a.Status,
                    a.IsSpecialAppointment,
                    ReminderSent = a.ReminderSent
                })
                .ToListAsync();

            return Ok(items);
        }
    }
}
