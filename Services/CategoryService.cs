using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UniKnowledge.Data;
using UniKnowledge.DTOs.Category;
using UniKnowledge.Models;

namespace UniKnowledge.Services;

public class CategoryService : ICategoryService
{
    private readonly AppDbContext _context;

    public CategoryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(bool Success, string Message, int Count, object? Categories)> SeedCategoriesAsync()
    {
        if (await _context.Categories.AnyAsync())
        {
            return (false, "Categories already exist. Delete them first if you want to reseed.", 0, null);
        }

        var categories = new List<Category>
        {
            new Category { CategoryName = "Programming", Description = "Questions about programming languages, algorithms, and software development", Slug = "programming" },
            new Category { CategoryName = "Web Development", Description = "Frontend, backend, and full-stack web development questions", Slug = "web-development" },
            new Category { CategoryName = "Database", Description = "Database design, SQL, NoSQL, and data management", Slug = "database" },
            new Category { CategoryName = "DevOps", Description = "CI/CD, deployment, containerization, and infrastructure", Slug = "devops" },
            new Category { CategoryName = "Mobile Development", Description = "iOS, Android, React Native, and mobile app development", Slug = "mobile-development" },
            new Category { CategoryName = "Data Science", Description = "Machine learning, AI, data analysis, and statistics", Slug = "data-science" },
            new Category { CategoryName = "Security", Description = "Cybersecurity, encryption, authentication, and secure coding", Slug = "security" },
            new Category { CategoryName = "Other", Description = "General questions and topics that don't fit other categories", Slug = "other" }
        };

        _context.Categories.AddRange(categories);
        await _context.SaveChangesAsync();

        var categoriesResult = categories.Select(c => new
        {
            c.CategoryId,
            c.CategoryName,
            c.Description,
            c.Slug
        });

        return (true, "Categories seeded successfully", categories.Count, categoriesResult);
    }

    public async Task<List<CategoryDto>> GetCategoriesAsync()
    {
        var categories = await _context.Categories
            .Include(c => c.Questions)
            .OrderBy(c => c.CategoryName)
            .ToListAsync();

        return categories.Select(c => new CategoryDto
        {
            CategoryId = c.CategoryId,
            CategoryName = c.CategoryName,
            Description = c.Description,
            Slug = c.Slug,
            QuestionCount = c.Questions.Count
        }).ToList();
    }

    public async Task<CategoryDto?> GetCategoryByIdAsync(int id)
    {
        var category = await _context.Categories
            .Include(c => c.Questions)
            .FirstOrDefaultAsync(c => c.CategoryId == id);

        if (category == null) return null;

        return new CategoryDto
        {
            CategoryId = category.CategoryId,
            CategoryName = category.CategoryName,
            Description = category.Description,
            Slug = category.Slug,
            QuestionCount = category.Questions.Count
        };
    }

    public async Task<(bool Success, string Message, CategoryDto? Category)> CreateCategoryAsync(CreateCategoryDto dto)
    {
        if (await _context.Categories.AnyAsync(c => c.CategoryName == dto.CategoryName))
        {
            return (false, "Category name already exists", null);
        }

        if (await _context.Categories.AnyAsync(c => c.Slug == dto.Slug))
        {
            return (false, "Slug already exists", null);
        }

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

        return (true, string.Empty, result);
    }

    public async Task<(bool Success, string Message, CategoryDto? Category)> UpdateCategoryAsync(int id, UpdateCategoryDto dto)
    {
        var category = await _context.Categories.FindAsync(id);

        if (category == null)
        {
            return (false, "Category not found", null);
        }

        if (!string.IsNullOrEmpty(dto.CategoryName) && 
            await _context.Categories.AnyAsync(c => c.CategoryName == dto.CategoryName && c.CategoryId != id))
        {
            return (false, "Category name already exists", null);
        }

        if (!string.IsNullOrEmpty(dto.Slug) && 
            await _context.Categories.AnyAsync(c => c.Slug == dto.Slug && c.CategoryId != id))
        {
            return (false, "Slug already exists", null);
        }

        if (!string.IsNullOrEmpty(dto.CategoryName))
            category.CategoryName = dto.CategoryName;

        if (dto.Description != null)
            category.Description = dto.Description;

        if (!string.IsNullOrEmpty(dto.Slug))
            category.Slug = dto.Slug;

        await _context.SaveChangesAsync();

        // Reload with question count
        var updatedCategory = await _context.Categories
            .Include(c => c.Questions)
            .FirstOrDefaultAsync(c => c.CategoryId == id);

        var result = new CategoryDto
        {
            CategoryId = updatedCategory!.CategoryId,
            CategoryName = updatedCategory.CategoryName,
            Description = updatedCategory.Description,
            Slug = updatedCategory.Slug,
            QuestionCount = updatedCategory.Questions.Count
        };

        return (true, string.Empty, result);
    }

    public async Task<(bool Success, string Message)> DeleteCategoryAsync(int id)
    {
        var category = await _context.Categories
            .Include(c => c.Questions)
            .FirstOrDefaultAsync(c => c.CategoryId == id);

        if (category == null)
        {
            return (false, "Category not found");
        }

        if (category.Questions.Any())
        {
            return (false, "Cannot delete category with existing questions. Please reassign or delete the questions first.");
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return (true, string.Empty);
    }
}
