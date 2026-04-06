using UniKnowledge.DTOs.Category;

namespace UniKnowledge.Services;

public interface ICategoryService
{
    Task<(bool Success, string Message, int Count, object? Categories)> SeedCategoriesAsync();
    Task<List<CategoryDto>> GetCategoriesAsync();
    Task<CategoryDto?> GetCategoryByIdAsync(int id);
    Task<(bool Success, string Message, CategoryDto? Category)> CreateCategoryAsync(CreateCategoryDto dto);
    Task<(bool Success, string Message, CategoryDto? Category)> UpdateCategoryAsync(int id, UpdateCategoryDto dto);
    Task<(bool Success, string Message)> DeleteCategoryAsync(int id);
}
