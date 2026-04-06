using System.Collections.Generic;
using System.Threading.Tasks;
using UniKnowledge.DTOs.Shared;
using UniKnowledge.DTOs.Tag;
using UniKnowledge.DTOs.Question;

namespace UniKnowledge.Services;

public interface ITagService
{
    Task<(bool Success, string Message, int Count, object? Tags)> SeedTagsAsync();
    Task<(bool Success, string Message, TagResponseDto? Tag)> CreateTagAsync(CreateTagDto dto);
    Task<List<TagResponseDto>> GetTagsAsync(string? search);
    Task<TagResponseDto?> GetTagByIdAsync(int id);
    Task<List<TagResponseDto>> GetPopularTagsAsync(int limit);
    Task<(bool Success, string Message, string? TagName, List<QuestionSummaryDto>? Items, PageInfo? PageInfo)> GetQuestionsByTagAsync(int id, int limit, string? after);
    Task<List<TagResponseDto>> SuggestTagsAsync(string? query, int limit);
    Task<List<TagResponseDto>> GetTrendingTagsAsync(int days, int limit);
    Task<(List<QuestionSummaryDto> Items, PageInfo PageInfo)> FilterQuestionsByTagsAsync(TagFilterDto dto);
    Task<(bool Success, string Message, TagResponseDto? Tag)> UpdateTagAsync(int id, UpdateTagDto dto);
    Task<(bool Success, string Message)> DeleteTagAsync(int id);
}
