using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using race_day_backend_api.Data;
using race_day_backend_api.Models;

namespace race_day_backend_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CategoriesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }


        [HttpGet]
        [Authorize(Roles = "Participant,Organiser")]
        public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.Title)
                .ToListAsync();

            return Ok(categories);
        }


        [HttpGet("{id:int}")]
        [Authorize(Roles = "Participant,Organiser")]
        public async Task<ActionResult<Category>> GetCategory(int id)
        {
            var category = await _context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return NotFound(new
                {
                    message = "Category not found."
                });
            }

            return Ok(category);
        }


        
        [HttpGet("{id:int}/events")]
        [Authorize(Roles = "Participant,Organiser")]
        public async Task<IActionResult> GetCategoryEvents(int id)
        {
            var category = await _context.Categories
                .AsNoTracking()
                .Include(c => c.Events)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return NotFound(new
                {
                    message = "Category not found."
                });
            }

            return Ok(new
            {
                category.CategoryId,
                category.Title,
                category.Description,
                events = category.Events.Select(e => new
                {
                    e.EventId,
                    e.Title,
                    e.Description,
                    e.StartLocation,
                    e.EndLocation,
                    e.StartTime,
                    e.Distance,
                    e.Date,
                    e.ImageUrl
                })
            });
        }


        
        [HttpPost]
        [Authorize(Roles = "Organiser")]
        public async Task<ActionResult<Category>> CreateCategory(
            [FromBody] Category category)
        {
            // Validate title
            if (string.IsNullOrWhiteSpace(category.Title))
            {
                return BadRequest(new
                {
                    message = "Category title is required."
                });
            }

            // Clean input
            category.Title = category.Title.Trim();

            if (!string.IsNullOrWhiteSpace(category.Description))
            {
                category.Description = category.Description.Trim();
            }

            // Check for duplicate category
            var categoryExists = await _context.Categories
                .AnyAsync(c =>
                    c.Title.ToLower() == category.Title.ToLower());

            if (categoryExists)
            {
                return Conflict(new
                {
                    message = "A category with this title already exists."
                });
            }

            // Only allow fields that belong to a new category.
            // Do not accept CategoryId from the client.
            category.CategoryId = 0;

            _context.Categories.Add(category);

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetCategory),
                new { id = category.CategoryId },
                category
            );
        }


        
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> UpdateCategory(
            int id,
            [FromBody] Category category)
        {
            // Make sure the URL ID and body ID match
            if (id != category.CategoryId)
            {
                return BadRequest(new
                {
                    message = "The category ID in the URL does not match the category ID in the request."
                });
            }

            if (string.IsNullOrWhiteSpace(category.Title))
            {
                return BadRequest(new
                {
                    message = "Category title is required."
                });
            }

            var existingCategory = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (existingCategory == null)
            {
                return NotFound(new
                {
                    message = "Category not found."
                });
            }

            category.Title = category.Title.Trim();

            if (!string.IsNullOrWhiteSpace(category.Description))
            {
                category.Description = category.Description.Trim();
            }

            // Check whether another category already uses this title
            var duplicateExists = await _context.Categories
                .AnyAsync(c =>
                    c.CategoryId != id &&
                    c.Title.ToLower() == category.Title.ToLower());

            if (duplicateExists)
            {
                return Conflict(new
                {
                    message = "Another category with this title already exists."
                });
            }

            // Update only editable properties
            existingCategory.Title = category.Title;
            existingCategory.Description = category.Description;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Category updated successfully.",
                category = existingCategory
            });
        }


        
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Events)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return NotFound(new
                {
                    message = "Category not found."
                });
            }


            
            if (category.Events.Any())
            {
                return Conflict(new
                {
                    message = "This category cannot be deleted because it is being used by one or more events.",
                    eventCount = category.Events.Count
                });
            }


            _context.Categories.Remove(category);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Category deleted successfully."
            });
        }
    }
}
