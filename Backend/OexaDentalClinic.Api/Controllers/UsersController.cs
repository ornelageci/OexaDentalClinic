using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OexaDentalClinic.Api.Data;
using OexaDentalClinic.Api.DTOs;
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
        public async Task<IActionResult> GetAll([FromQuery] string? role)
        {
            var query = _db.Users.AsQueryable();
            if (!string.IsNullOrWhiteSpace(role))
                query = query.Where(u => u.Role == role.Trim());

            var users = await query
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

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var allowed = new[] { "Patient", "Dentist", "Manager", "Marketer", "Admin" };
            if (!allowed.Contains(dto.Role))
                return BadRequest(new { error = "Invalid role." });

            var email = dto.Email.Trim().ToLower();
            if (await _db.Users.AnyAsync(u => u.Email.ToLower() == email))
                return BadRequest(new { error = "Email already exists." });

            var user = new User
            {
                Email = dto.Email.Trim(),
                Password = dto.Password,
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                PhoneNumber = dto.PhoneNumber?.Trim(),
                Role = dto.Role,
                DentistServiceKey = dto.Role == "Dentist" ? dto.DentistServiceKey?.Trim() : null
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.Role,
                user.DentistServiceKey
            });
        }

        [HttpGet("dentists")]
        public async Task<IActionResult> GetDentists([FromQuery] string? problemKey)
        {
            var query = _db.Users.Where(u => u.Role == "Dentist");

            if (!string.IsNullOrWhiteSpace(problemKey))
            {
                var problem = await _db.DentalProblems.FirstOrDefaultAsync(p => p.Key == problemKey.Trim());
                if (problem != null)
                    query = query.Where(u => u.DentistServiceKey == problem.DentistCategoryKey);
            }

            var dentists = await query
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
