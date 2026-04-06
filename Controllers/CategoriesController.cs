using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniKnowledge.DTOs.Category;
using UniKnowledge.Services;

namespace UniKnowledge.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpPost("seed")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> SeedCategories()
    {
        var result = await _categoryService.SeedCategoriesAsync();
        
        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return Ok(new
        {
            message = result.Message,
            count = result.Count,
            categories = result.Categories
        });
    }

    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> GetCategories()
    {
        var result = await _categoryService.GetCategoriesAsync();
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CategoryDto>> GetCategory(int id)
    {
        var result = await _categoryService.GetCategoryByIdAsync(id);

        if (result == null)
        {
            return NotFound(new { message = "Category not found" });
        }

        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<CategoryDto>> CreateCategory([FromBody] CreateCategoryDto dto)
    {
        var result = await _categoryService.CreateCategoryAsync(dto);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return CreatedAtAction(nameof(GetCategory), new { id = result.Category!.CategoryId }, result.Category);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<ActionResult<CategoryDto>> UpdateCategory(int id, [FromBody] UpdateCategoryDto dto)
    {
        var result = await _categoryService.UpdateCategoryAsync(id, dto);

        if (!result.Success)
        {
            if (result.Message == "Category not found")
            {
                return NotFound(new { message = result.Message });
            }
            return BadRequest(new { message = result.Message });
        }

        return Ok(result.Category);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteCategory(int id)
    {
        var result = await _categoryService.DeleteCategoryAsync(id);

        if (!result.Success)
        {
            if (result.Message == "Category not found")
            {
                return NotFound(new { message = result.Message });
            }
            return BadRequest(new { message = result.Message });
        }

        return NoContent();
    }
}
