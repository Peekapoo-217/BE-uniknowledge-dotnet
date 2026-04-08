namespace UniKnowledge.DTOs.Shared;

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
