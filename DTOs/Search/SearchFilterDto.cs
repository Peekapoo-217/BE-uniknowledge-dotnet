namespace UniKnowledge.DTOs.Search;

public record SearchFilterDto(
    string? Keyword,
    int? CategoryId,
    string? Tag,
    bool? IsSolved
);
