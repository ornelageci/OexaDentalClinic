using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OexaDentalClinic.Api.Data;
using OexaDentalClinic.Api.DTOs;
using OexaDentalClinic.Api.Models;
using System.Text.RegularExpressions;

namespace OexaDentalClinic.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserRolesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public UserRolesController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var roles = await _db.UserRoleDefinitions
                .OrderBy(r => r.SortOrder)
                .ThenBy(r => r.DisplayName)
                .Select(r => new { r.Id, r.Key, r.DisplayName, r.IsSystem })
                .ToListAsync();
            return Ok(roles);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RoleDefinitionDto dto)
        {
            var key = NormalizeKey(dto.Key ?? dto.DisplayName);
            if (string.IsNullOrEmpty(key))
                return BadRequest(new { error = "Role key is required." });

            if (await _db.UserRoleDefinitions.AnyAsync(r => r.Key == key))
                return BadRequest(new { error = "This role already exists." });

            var maxOrder = await _db.UserRoleDefinitions.MaxAsync(r => (int?)r.SortOrder) ?? 0;
            var role = new UserRoleDefinition
            {
                Key = key,
                DisplayName = dto.DisplayName.Trim(),
                SortOrder = maxOrder + 1,
                IsSystem = false
            };

            _db.UserRoleDefinitions.Add(role);
            await _db.SaveChangesAsync();

            return Ok(new { role.Id, role.Key, role.DisplayName, role.IsSystem });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var role = await _db.UserRoleDefinitions.FindAsync(id);
            if (role == null) return NotFound();

            if (role.IsSystem)
                return BadRequest(new { error = "System roles cannot be deleted." });

            if (await _db.Users.AnyAsync(u => u.Role == role.Key))
                return BadRequest(new { error = "Users still have this role. Reassign them first." });

            _db.UserRoleDefinitions.Remove(role);
            await _db.SaveChangesAsync();
            return Ok();
        }

        private static string NormalizeKey(string input)
        {
            var cleaned = Regex.Replace(input.Trim(), @"[^a-zA-Z0-9\s]", "");
            var parts = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "";
            return string.Concat(parts.Select(p =>
                char.ToUpperInvariant(p[0]) + p[1..].ToLowerInvariant()));
        }
    }
}
