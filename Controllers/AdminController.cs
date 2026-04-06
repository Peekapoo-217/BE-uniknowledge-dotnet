using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UniKnowledge.Data;
using UniKnowledge.DTOs.Category;
using UniKnowledge.Models;

namespace UniKnowledge.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;

    public AdminController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("categories")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CategoryDto>> CreateCategory([FromBody] CategoryDto dto)
    {
        // Double check role (extra security)
        var userRole = User.FindFirstValue(ClaimTypes.Role);
        if (userRole != "Admin")
        {
            return StatusCode(403, new { message = "Only Admin users can create categories" });
        }

        // Check if category name already exists
        if (await _context.Categories.AnyAsync(c => c.CategoryName == dto.CategoryName))
        {
            return BadRequest(new { message = "Category name already exists" });
        }

        // Ignore CategoryId and QuestionCount from input (security)
        var category = new Category
        {
            CategoryName = dto.CategoryName,
            Description = dto.Description,
            Slug = dto.Slug
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var result = new CategoryDto
        {
            CategoryId = category.CategoryId,
            CategoryName = category.CategoryName,
            Description = category.Description,
            Slug = category.Slug,
            QuestionCount = 0
        };

        return CreatedAtAction(nameof(GetCategories), new { id = category.CategoryId }, result);
    }

    [HttpPut("categories/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CategoryDto>> UpdateCategory(int id, [FromBody] CategoryDto dto)
    {
        var userRole = User.FindFirstValue(ClaimTypes.Role);
        if (userRole != "Admin")
        {
            return StatusCode(403, new { message = "Only Admin users can update categories" });
        }

        var category = await _context.Categories.FindAsync(id);
        if (category == null)
        {
            return NotFound(new { message = "Category not found" });
        }

        // Check if category name already exists (excluding current category)
        if (await _context.Categories.AnyAsync(c => c.CategoryName == dto.CategoryName && c.CategoryId != id))
        {
            return BadRequest(new { message = "Category name already exists" });
        }

        category.CategoryName = dto.CategoryName;
        category.Description = dto.Description;
        category.Slug = dto.Slug;

        await _context.SaveChangesAsync();

        var result = new CategoryDto
        {
            CategoryId = category.CategoryId,
            CategoryName = category.CategoryName,
            Description = category.Description,
            Slug = category.Slug,
            QuestionCount = await _context.Questions.CountAsync(q => q.CategoryId == id)
        };

        return Ok(result);
    }

    [HttpDelete("categories/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DeleteCategory(int id)
    {
        var userRole = User.FindFirstValue(ClaimTypes.Role);
        if (userRole != "Admin")
        {
            return StatusCode(403, new { message = "Only Admin users can delete categories" });
        }

        var category = await _context.Categories
            .Include(c => c.Questions)
            .FirstOrDefaultAsync(c => c.CategoryId == id);

        if (category == null)
        {
            return NotFound(new { message = "Category not found" });
        }

        // Check if category has questions
        if (category.Questions.Any())
        {
            return BadRequest(new { message = "Cannot delete category that has questions" });
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("categories")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<CategoryDto>>> GetCategories()
    {
        var categories = await _context.Categories
            .Include(c => c.Questions)
            .OrderBy(c => c.CategoryName)
            .ToListAsync();

        var result = categories.Select(c => new CategoryDto
        {
            CategoryId = c.CategoryId,
            CategoryName = c.CategoryName,
            Description = c.Description,
            Slug = c.Slug,
            QuestionCount = c.Questions.Count
        }).ToList();

        return Ok(result);
    }
}

