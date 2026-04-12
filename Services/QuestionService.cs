using Microsoft.EntityFrameworkCore;
using UniKnowledge.Data;
using UniKnowledge.DTOs.Question;
using UniKnowledge.DTOs.Shared;
using UniKnowledge.Helpers;
using UniKnowledge.Models;
using UniKnowledge.Constants;

namespace UniKnowledge.Services;

public interface IQuestionService
{
    Task<CursorPagedResult<QuestionSummaryDto>> GetQuestionsAsync(string? search = null, int? categoryId = null, int? tagId = null, string? status = null, int limit = 20, string? after = null);
    Task<QuestionResponseDto?> GetQuestionByIdAsync(int id);
    Task<string?> GetQuestionCodeAsync(int id);
    Task<QuestionResponseDto> CreateQuestionAsync(CreateQuestionDto dto, int userId);
    Task<QuestionResponseDto?> UpdateQuestionAsync(int id, UpdateQuestionDto dto, int userId);
    Task<bool> DeleteQuestionAsync(int id, int userId);
    Task<bool> IncrementViewCountAsync(int id);
}

public class QuestionService : IQuestionService
{
    private readonly AppDbContext _context;
    private readonly IFileUploadService _fileUploadService;
    private readonly ISearchService _searchService;

    public QuestionService(AppDbContext context, IFileUploadService fileUploadService, ISearchService searchService)
    {
        _context = context;
        _fileUploadService = fileUploadService;
        _searchService = searchService;
    }

    public async Task<CursorPagedResult<QuestionSummaryDto>> GetQuestionsAsync(string? search = null, int? categoryId = null, int? tagId = null, string? status = null, int limit = 20, string? after = null)
    {
        // If a search term is provided, use the Smart Search engine (Elasticsearch/SQL Fallback)
        if (!string.IsNullOrWhiteSpace(search))
        {
            var filter = new UniKnowledge.DTOs.Search.SearchFilterDto(
                search,
                categoryId,
                null, // Combined tag search is handled specifically in QuestionService.loadQuestionsWithMultipleTags on FE
                status == QuestionStatus.Closed // Basic mapping for IsSolved
            );

            var searchResults = await _searchService.AdvancedSearchAsync(filter);
            var ids = searchResults.Select(r => r.Id).ToList();

            if (!ids.Any())
            {
                return new CursorPagedResult<QuestionSummaryDto> { Items = new List<QuestionSummaryDto>(), PageInfo = new PageInfo { HasNextPage = false } };
            }

            // Fetch full models and maintain Relevance Order from search scores
            var questions = await _context.Questions
                .Include(q => q.User)
                .Include(q => q.Category)
                .Include(q => q.QuestionTags)
                    .ThenInclude(qt => qt.Tag)
                .Include(q => q.Answers)
                .Include(q => q.Votes)
                .Where(q => ids.Contains(q.QuestionId))
                .ToListAsync();

            var sortedDtos = ids
                .Select(id => questions.FirstOrDefault(q => q.QuestionId == id))
                .Where(q => q != null)
                .Select(q => MapToSummaryDto(q!))
                .ToList();

            return new CursorPagedResult<QuestionSummaryDto>
            {
                Items = sortedDtos,
                PageInfo = new PageInfo { HasNextPage = false, EndCursor = null } // Pagination for search results is simplified for now
            };
        }

        // Standard LINQ query for main feed (no search term)
        var query = _context.Questions
            .Include(q => q.User)
            .Include(q => q.Category)
            .Include(q => q.QuestionTags)
                .ThenInclude(qt => qt.Tag)
            .Include(q => q.Answers)
            .Include(q => q.Votes)
            .AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(q => q.CategoryId == categoryId.Value);
        }

        if (tagId.HasValue)
        {
            query = query.Where(q => q.QuestionTags.Any(qt => qt.TagId == tagId.Value));
        }

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(q => q.Status == status);
        }
        else
        {
            query = query.Where(q => q.Status != QuestionStatus.Hidden);
        }

        // Apply cursor filter
        var cursor = CursorHelper.Decode(after);
        if (cursor.HasValue)
        {
            var (cursorDate, cursorId) = cursor.Value;
            query = query.Where(q => q.CreatedAt < cursorDate ||
                        (q.CreatedAt == cursorDate && q.QuestionId < cursorId));
        }

        // Order by created date (newest first), then by Id for stable sort
        query = query.OrderByDescending(q => q.CreatedAt)
                     .ThenByDescending(q => q.QuestionId);

        // Fetch limit + 1 to determine hasNextPage
        var pagedQuestions = await query.Take(limit + 1).ToListAsync();
        var hasNextPage = pagedQuestions.Count > limit;
        var items = pagedQuestions.Take(limit).ToList();

        var dtos = items.Select(q => MapToSummaryDto(q)).ToList();

        return new CursorPagedResult<QuestionSummaryDto>
        {
            Items = dtos,
            PageInfo = new PageInfo
            {
                HasNextPage = hasNextPage,
                EndCursor = items.Any() ? CursorHelper.Encode(items.Last().CreatedAt, items.Last().QuestionId) : null
            }
        };
    }

    public async Task<QuestionResponseDto?> GetQuestionByIdAsync(int id)
    {
        var question = await _context.Questions
            .Include(q => q.User)
            .Include(q => q.Category)
            .Include(q => q.QuestionTags)
                .ThenInclude(qt => qt.Tag)
            .Include(q => q.Answers)
            .Include(q => q.Votes)
            .FirstOrDefaultAsync(q => q.QuestionId == id);

        return question == null ? null : MapToDto(question);
    }

    public async Task<string?> GetQuestionCodeAsync(int id)
    {
        return await _context.Questions
            .Where(q => q.QuestionId == id)
            .Select(q => q.CodeContent)
            .FirstOrDefaultAsync();
    }

    private async Task SyncToElasticsearchAsync(int questionId)
    {
        try
        {
            var question = await _context.Questions
                .Include(q => q.QuestionTags)
                    .ThenInclude(qt => qt.Tag)
                .Include(q => q.Votes)
                .FirstOrDefaultAsync(q => q.QuestionId == questionId);

            if (question != null)
            {
                var doc = new UniKnowledge.Models.Indexes.QuestionIndexDocument
                {
                    Id = question.QuestionId,
                    Title = question.Title,
                    Content = question.Content ?? string.Empty,
                    Tags = question.QuestionTags.Select(qt => qt.Tag.TagName).ToList(),
                    Upvotes = question.Votes.Count(v => v.VoteType == 1) - question.Votes.Count(v => v.VoteType == -1),
                    IsSolved = question.Status == QuestionStatus.Closed,
                    CategoryId = question.CategoryId
                };
                await _searchService.IndexQuestionAsync(doc);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error syncing to Elasticsearch: {ex.Message}");
        }
    }

    public async Task<QuestionResponseDto> CreateQuestionAsync(CreateQuestionDto dto, int userId)
    {
        var lineCount = string.IsNullOrEmpty(dto.CodeContent) ? 0 : dto.CodeContent.Split('\n').Length;
        var question = new Question
        {
            UserId = userId,
            Title = dto.Title,
            Content = dto.Content,
            CategoryId = dto.CategoryId,
            ImageUrl = dto.ImageUrl,
            FileUrl = dto.FileUrl,
            CodeContent = dto.CodeContent,
            CodeLanguage = dto.CodeLanguage,
            CodeLineCount = lineCount,
            Status = QuestionStatus.Open,
            CreatedAt = DateTime.UtcNow
        };

        _context.Questions.Add(question);
        await _context.SaveChangesAsync();

        // Add tags
        if (dto.TagIds.Any())
        {
            var questionTags = dto.TagIds.Select(tagId => new QuestionTag
            {
                QuestionId = question.QuestionId,
                TagId = tagId
            }).ToList();

            _context.QuestionTags.AddRange(questionTags);
            await _context.SaveChangesAsync();
        }

        var result = (await GetQuestionByIdAsync(question.QuestionId))!;
        await SyncToElasticsearchAsync(question.QuestionId);
        return result;
    }

    public async Task<QuestionResponseDto?> UpdateQuestionAsync(int id, UpdateQuestionDto dto, int userId)
    {
        var question = await _context.Questions
            .Include(q => q.QuestionTags)
            .FirstOrDefaultAsync(q => q.QuestionId == id);

        if (question == null || question.UserId != userId)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(dto.Title))
            question.Title = dto.Title;

        if (!string.IsNullOrEmpty(dto.Content))
            question.Content = dto.Content;

        if (dto.CategoryId.HasValue)
            question.CategoryId = dto.CategoryId.Value;

        if (dto.ImageUrl != null)
            question.ImageUrl = dto.ImageUrl;

        if (dto.FileUrl != null)
            question.FileUrl = dto.FileUrl;

        if (dto.CodeContent != null)
        {
            question.CodeContent = dto.CodeContent;
            question.CodeLineCount = dto.CodeContent.Split('\n').Length;
        }

        if (dto.CodeLanguage != null)
            question.CodeLanguage = dto.CodeLanguage;

        if (!string.IsNullOrEmpty(dto.Status))
            question.Status = dto.Status;

        if (dto.TagIds != null)
        {
            // Remove old tags
            _context.QuestionTags.RemoveRange(question.QuestionTags);

            // Add new tags
            var questionTags = dto.TagIds.Select(tagId => new QuestionTag
            {
                QuestionId = question.QuestionId,
                TagId = tagId
            }).ToList();

            _context.QuestionTags.AddRange(questionTags);
        }

        question.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await SyncToElasticsearchAsync(id);

        return await GetQuestionByIdAsync(id);
    }

    public async Task<bool> DeleteQuestionAsync(int id, int userId)
    {
        try
        {
            var question = await _context.Questions
                .Include(q => q.QuestionTags)
                .Include(q => q.Votes)
                .Include(q => q.Answers)
                    .ThenInclude(a => a.Votes)
                .FirstOrDefaultAsync(q => q.QuestionId == id);

            if (question == null || question.UserId != userId)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(question.FileUrl))
            {
                try
                {
                    await _fileUploadService.DeleteFileAsync(question.FileUrl);
                }
                catch
                {
                    // Continue with question deletion even if file deletion fails
                }
            }

            // Delete answer votes first
            if (question.Answers != null && question.Answers.Any())
            {
                foreach (var answer in question.Answers)
                {
                    if (answer.Votes != null && answer.Votes.Any())
                    {
                        _context.Votes.RemoveRange(answer.Votes);
                    }
                }
                // Then delete answers
                _context.Answers.RemoveRange(question.Answers);
            }

            if (question.QuestionTags != null && question.QuestionTags.Any())
            {
                _context.QuestionTags.RemoveRange(question.QuestionTags);
            }

            if (question.Votes != null && question.Votes.Any())
            {
                _context.Votes.RemoveRange(question.Votes);
            }

            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();

            await _searchService.DeleteQuestionAsync(id);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> IncrementViewCountAsync(int id)
    {
        var question = await _context.Questions.FirstOrDefaultAsync(q => q.QuestionId == id);

        if (question == null)
        {
            return false;
        }

        question.ViewCount++;
        await _context.SaveChangesAsync();
        return true;
    }

    public QuestionSummaryDto MapToSummaryDto(Question q)
    {
        return new QuestionSummaryDto
        {
            QuestionId = q.QuestionId,
            Title = q.Title,
            Content = q.Content != null ? (q.Content.Length > 200 ? q.Content.Substring(0, 200) + "..." : q.Content) : string.Empty,
            ViewCount = q.ViewCount,
            Status = q.Status,
            ImageUrl = q.ImageUrl,
            FileUrl = q.FileUrl,
            CodeLanguage = q.CodeLanguage,
            CodeLineCount = q.CodeLineCount,
            CreatedAt = q.CreatedAt,
            UpdatedAt = q.UpdatedAt,
            User = new UserSummaryDto { Username = q.User.Username, AvatarUrl = q.User.AvatarUrl },
            Category = q.Category != null ? new CategorySummaryDto { CategoryId = q.Category.CategoryId, CategoryName = q.Category.CategoryName } : null,
            Tags = q.QuestionTags.Select(qt => new TagSummaryDto
            {
                TagId = qt.TagId,
                TagName = qt.Tag.TagName
            }).ToList(),
            AnswerCount = q.Answers.Count,
            VoteCount = q.Votes.Sum(v => v.VoteType),
            HasAcceptedAnswer = q.Answers.Any(a => a.IsAccepted)
        };
    }

    public QuestionResponseDto MapToDto(Question q)
    {
        // Optimization: Only return full code if it's "short" (< 20 lines)
        // Otherwise, FE will fetch it via the /code endpoint
        var shouldIncludeCode = q.CodeLineCount < 20;

        return new QuestionResponseDto
        {
            QuestionId = q.QuestionId,
            Title = q.Title,
            Content = q.Content,
            ViewCount = q.ViewCount,
            Status = q.Status,
            ImageUrl = q.ImageUrl,
            FileUrl = q.FileUrl,
            CodeContent = shouldIncludeCode ? q.CodeContent : null,
            CodeLanguage = q.CodeLanguage,
            CodeLineCount = q.CodeLineCount,
            CreatedAt = q.CreatedAt,
            UpdatedAt = q.UpdatedAt,
            UserId = q.UserId,
            Username = q.User.Username,
            AvatarUrl = q.User.AvatarUrl,
            CategoryId = q.CategoryId,
            CategoryName = q.Category.CategoryName,
            Tags = q.QuestionTags.Select(qt => new TagDto
            {
                TagId = qt.TagId,
                TagName = qt.Tag.TagName
            }).ToList(),
            AnswerCount = q.Answers.Count,
            VoteCount = q.Votes.Sum(v => v.VoteType),
            HasAcceptedAnswer = q.Answers.Any(a => a.IsAccepted)
        };
    }
}
