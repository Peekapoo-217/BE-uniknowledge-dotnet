using System;
using System.Collections.Generic;

namespace UniKnowledge.DTOs.Question;

public class QuestionSummaryDto
{
    public int QuestionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int ViewCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    
    public UserSummaryDto User { get; set; } = null!;
    public CategorySummaryDto? Category { get; set; }
    public IEnumerable<TagSummaryDto> Tags { get; set; } = new List<TagSummaryDto>();
    
    public int AnswerCount { get; set; }
    public int VoteCount { get; set; }
}

public class UserSummaryDto
{
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}

public class CategorySummaryDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
}

public class TagSummaryDto
{
    public int TagId { get; set; }
    public string TagName { get; set; } = string.Empty;
}
