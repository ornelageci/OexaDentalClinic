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
    public class DentistCategoriesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public DentistCategoriesController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _db.DentistCategories
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.DisplayName)
                .Select(c => new { c.Id, c.Key, c.DisplayName })
                .ToListAsync();
            return Ok(items);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RoleDefinitionDto dto)
        {
            var key = NormalizeCategoryKey(dto.Key ?? dto.DisplayName);
            if (string.IsNullOrEmpty(key))
                return BadRequest(new { error = "Category key is required." });

            if (await _db.DentistCategories.AnyAsync(c => c.Key == key))
                return BadRequest(new { error = "This dentist type already exists." });

            var maxOrder = await _db.DentistCategories.MaxAsync(c => (int?)c.SortOrder) ?? 0;
            var category = new DentistCategory
            {
                Key = key,
                DisplayName = dto.DisplayName.Trim(),
                SortOrder = maxOrder + 1
            };

            _db.DentistCategories.Add(category);
            await _db.SaveChangesAsync();

            return Ok(new { category.Id, category.Key, category.DisplayName });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _db.DentistCategories.FindAsync(id);
            if (category == null) return NotFound();

            if (await _db.Users.AnyAsync(u => u.Role == "Dentist" && u.DentistServiceKey == category.Key))
                return BadRequest(new { error = "Dentists are still assigned to this type." });

            if (await _db.DentalProblems.AnyAsync(p => p.DentistCategoryKey == category.Key))
                return BadRequest(new { error = "Treatments still use this type. Change them first." });

            _db.DentistCategories.Remove(category);
            await _db.SaveChangesAsync();
            return Ok();
        }

        internal static string NormalizeCategoryKey(string input)
        {
            return Regex.Replace(input.Trim().ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
        }
    }
}
