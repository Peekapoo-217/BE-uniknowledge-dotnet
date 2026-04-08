using System.ComponentModel.DataAnnotations;

namespace UniKnowledge.DTOs.Question;

public class CreateQuestionDto
{
    [Required]
    [StringLength(255, MinimumLength = 10)]
    public string Title { get; set; } = string.Empty;

    public string? Content { get; set; } = string.Empty;

    [Required(ErrorMessage = "Category is required")]
    public int CategoryId { get; set; }

    public List<int> TagIds { get; set; } = new();

    public string? ImageUrl { get; set; }
    
    public string? FileUrl { get; set; }

    public string? CodeContent { get; set; }

    public string? CodeLanguage { get; set; }
}
