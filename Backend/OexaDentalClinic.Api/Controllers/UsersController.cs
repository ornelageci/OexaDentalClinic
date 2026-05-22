using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OexaDentalClinic.Api.Data;
using OexaDentalClinic.Api.Models;

namespace OexaDentalClinic.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _db;

        public UsersController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _db.Users
                .OrderBy(u => u.Role)
                .ThenBy(u => u.LastName)
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    u.PhoneNumber,
                    u.Role,
                    u.DentistServiceKey
                })
                .ToListAsync();
            return Ok(users);
        }

        [HttpGet("dentists")]
        public async Task<IActionResult> GetDentists()
        {
            var dentists = await _db.Users
                .Where(u => u.Role == "Dentist")
                .Select(u => new
                {
                    u.Id,
                    u.FirstName,
                    u.LastName,
                    u.DentistServiceKey,
                    Name = u.FirstName + " " + u.LastName
                })
                .ToListAsync();
            return Ok(dentists);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();
            if (user.Role == "Admin")
                return BadRequest(new { error = "Cannot delete admin account." });

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
            return Ok();
        }
    }
}
