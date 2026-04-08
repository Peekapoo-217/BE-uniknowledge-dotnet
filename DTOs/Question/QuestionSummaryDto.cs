using UniKnowledge.DTOs.Shared;

namespace UniKnowledge.DTOs.Question;

public class QuestionSummaryDto
{
    public int QuestionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; } = string.Empty;
    public int ViewCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? FileUrl { get; set; }
    public string? CodeLanguage { get; set; }
    public int CodeLineCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Nested info for cleaner structure
    public UserSummaryDto User { get; set; } = new();
    public CategorySummaryDto? Category { get; set; }
    public List<TagSummaryDto> Tags { get; set; } = new();

    // Stats
    public int AnswerCount { get; set; }
    public int VoteCount { get; set; }
    public bool HasAcceptedAnswer { get; set; }
}
