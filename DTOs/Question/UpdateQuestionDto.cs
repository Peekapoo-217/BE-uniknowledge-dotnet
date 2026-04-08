using System.ComponentModel.DataAnnotations;

namespace UniKnowledge.DTOs.Question;

public class UpdateQuestionDto
{
    [StringLength(255, MinimumLength = 10)]
    public string? Title { get; set; }

    public string? Content { get; set; }

    public int? CategoryId { get; set; }

    public List<int>? TagIds { get; set; }

    public string? ImageUrl { get; set; }
    
    public string? FileUrl { get; set; }

    public string? CodeContent { get; set; }

    public string? CodeLanguage { get; set; }

    public string? Status { get; set; } // Open, Closed, Hidden
}

