using System.ComponentModel.DataAnnotations;

namespace UniKnowledge.DTOs.Answer;

public class UpdateAnswerDto
{
    [Required]
    [MinLength(20)]
    public string Content { get; set; } = string.Empty;
    public string? CodeContent { get; set; }
    public string? CodeLanguage { get; set; }
}

