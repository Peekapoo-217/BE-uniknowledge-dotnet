using System.ComponentModel.DataAnnotations;

namespace UniKnowledge.DTOs.Answer;

public class CreateAnswerDto
{
    [Required]
    public string Content { get; set; } = string.Empty;
    public string? CodeContent { get; set; }
    public string? CodeLanguage { get; set; }
    public int? ParentId { get; set; }
}

