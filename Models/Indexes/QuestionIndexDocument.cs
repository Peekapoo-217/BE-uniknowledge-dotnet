using System.Collections.Generic;

namespace UniKnowledge.Models.Indexes;

public class QuestionIndexDocument
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public int Upvotes { get; set; }
    public bool IsSolved { get; set; }
    public int CategoryId { get; set; }
}
