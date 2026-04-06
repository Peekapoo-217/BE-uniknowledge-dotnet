namespace UniKnowledge.DTOs.Tag;

public class TagFilterDto
{
    public List<int>? TagIds { get; set; } = new();
    public string Logic { get; set; } = "AND"; // AND hoặc OR
    public int Limit { get; set; } = 20;
    public string? After { get; set; }
}

