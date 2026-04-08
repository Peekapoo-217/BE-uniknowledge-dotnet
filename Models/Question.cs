namespace UniKnowledge.Models;

public class Question
{
    public int QuestionId { get; set; }
    public int UserId { get; set; }
    public int CategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; } = string.Empty;
    public int ViewCount { get; set; } = 0;
    public string Status { get; set; } = "Open"; // Open, Closed, Hidden
    public string? ImageUrl { get; set; }
    public string? FileUrl { get; set; }
    public string? CodeContent { get; set; }
    public string? CodeLanguage { get; set; }
    public int CodeLineCount { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public ICollection<Answer> Answers { get; set; } = new List<Answer>();
    public ICollection<Vote> Votes { get; set; } = new List<Vote>();
    public ICollection<QuestionTag> QuestionTags { get; set; } = new List<QuestionTag>();
}

