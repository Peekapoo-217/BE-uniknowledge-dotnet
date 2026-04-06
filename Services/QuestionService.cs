using Microsoft.EntityFrameworkCore;
using UniKnowledge.Data;
using UniKnowledge.DTOs.Question;
using UniKnowledge.DTOs.Shared;
using UniKnowledge.Helpers;
using UniKnowledge.Models;

namespace UniKnowledge.Services;

public interface IQuestionService
{
    Task<CursorPagedResult<QuestionResponseDto>> GetQuestionsAsync(string? search = null, int? categoryId = null, int? tagId = null, string? status = null, int limit = 20, string? after = null);
    Task<QuestionResponseDto?> GetQuestionByIdAsync(int id);
    Task<QuestionResponseDto> CreateQuestionAsync(CreateQuestionDto dto, int userId);
    Task<QuestionResponseDto?> UpdateQuestionAsync(int id, UpdateQuestionDto dto, int userId);
    Task<bool> DeleteQuestionAsync(int id, int userId);
    Task<bool> IncrementViewCountAsync(int id);
}

public class QuestionService : IQuestionService
{
    private readonly AppDbContext _context;
    private readonly IFileUploadService _fileUploadService;

    public QuestionService(AppDbContext context, IFileUploadService fileUploadService)
    {
        _context = context;
        _fileUploadService = fileUploadService;
    }

    public async Task<CursorPagedResult<QuestionResponseDto>> GetQuestionsAsync(string? search = null, int? categoryId = null, int? tagId = null, string? status = null, int limit = 20, string? after = null)
    {
        var query = _context.Questions
            .Include(q => q.User)
            .Include(q => q.Category)
            .Include(q => q.QuestionTags)
                .ThenInclude(qt => qt.Tag)
            .Include(q => q.Answers)
            .Include(q => q.Votes)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(q => q.Title.Contains(search) || q.Content.Contains(search));
        }

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
            query = query.Where(q => q.Status != "Hidden");
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
        var questions = await query.Take(limit + 1).ToListAsync();
        var hasNextPage = questions.Count > limit;
        var items = questions.Take(limit).ToList();

        var dtos = items.Select(q => MapToDto(q)).ToList();

        return new CursorPagedResult<QuestionResponseDto>
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

    public async Task<QuestionResponseDto> CreateQuestionAsync(CreateQuestionDto dto, int userId)
    {
        var question = new Question
        {
            UserId = userId,
            Title = dto.Title,
            Content = dto.Content,
            CategoryId = dto.CategoryId,
            ImageUrl = dto.ImageUrl,
            FileUrl = dto.FileUrl,
            Status = "Open",
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

        return (await GetQuestionByIdAsync(question.QuestionId))!;
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

    private QuestionResponseDto MapToDto(Question q)
    {
        return new QuestionResponseDto
        {
            QuestionId = q.QuestionId,
            Title = q.Title,
            Content = q.Content,
            ViewCount = q.ViewCount,
            Status = q.Status,
            ImageUrl = q.ImageUrl,
            FileUrl = q.FileUrl,
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

