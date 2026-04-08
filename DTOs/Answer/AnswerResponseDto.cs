namespace UniKnowledge.DTOs.Answer;

public class AnswerResponseDto
{
    public int AnswerId { get; set; }
    public int QuestionId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? CodeContent { get; set; }
    public string? CodeLanguage { get; set; }
    public int CodeLineCount { get; set; }
    public bool IsAccepted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? ParentId { get; set; }

    // Author info
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }

    // Stats
    public int VoteCount { get; set; }
}

