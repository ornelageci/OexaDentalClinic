using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OexaDentalClinic.Api.Data;

namespace OexaDentalClinic.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ReportsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var appointments = await _db.Appointments.ToListAsync();
            var receipts = await _db.Receipts.Where(r => r.Status == "Finalized").ToListAsync();

            var byService = appointments
                .GroupBy(a => a.ServiceNeeded)
                .Select(g => new { service = g.Key, count = g.Count() })
                .OrderByDescending(x => x.count)
                .ToList();

            return Ok(new
            {
                totalAppointments = appointments.Count,
                booked = appointments.Count(a => a.Status == "Booked"),
                inProgress = appointments.Count(a => a.Status == "InProgress"),
                completed = appointments.Count(a => a.Status == "Completed"),
                cancelled = appointments.Count(a => a.Status == "Cancelled"),
                totalRevenue = receipts.Sum(r => r.TotalAmount),
                receiptsCount = receipts.Count,
                appointmentsByService = byService
            });
        }
    }
}
