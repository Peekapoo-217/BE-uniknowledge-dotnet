using Microsoft.EntityFrameworkCore;
using UniKnowledge.Data;
using UniKnowledge.DTOs.Answer;
using UniKnowledge.DTOs.Shared;
using UniKnowledge.Helpers;
using UniKnowledge.Models;

namespace UniKnowledge.Services;

public interface IAnswerService
{
    Task<CursorPagedResult<AnswerResponseDto>> GetAnswersByQuestionIdAsync(int questionId, int limit = 20, string? after = null);
    Task<AnswerResponseDto> CreateAnswerAsync(int questionId, CreateAnswerDto dto, int userId);
    Task<string?> GetAnswerCodeAsync(int id);
    Task<AnswerResponseDto?> UpdateAnswerAsync(int id, UpdateAnswerDto dto, int userId);
    Task<bool> DeleteAnswerAsync(int id, int userId);
    Task<bool> AcceptAnswerAsync(int id, int userId);
}

public class AnswerService : IAnswerService
{
    private readonly AppDbContext _context;

    public AnswerService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CursorPagedResult<AnswerResponseDto>> GetAnswersByQuestionIdAsync(int questionId, int limit = 20, string? after = null)
    {
        var query = _context.Answers
            .Include(a => a.User)
            .Include(a => a.Votes)
            .Where(a => a.QuestionId == questionId);

        // Apply cursor filter
        var cursor = CursorHelper.Decode(after);
        if (cursor.HasValue)
        {
            var (cursorDate, cursorId) = cursor.Value;
            query = query.Where(a => a.CreatedAt < cursorDate ||
                        (a.CreatedAt == cursorDate && a.AnswerId < cursorId));
        }

        // Accepted answers first, then newest
        query = query.OrderByDescending(a => a.IsAccepted)
                     .ThenByDescending(a => a.CreatedAt)
                     .ThenByDescending(a => a.AnswerId);

        var answers = await query.Take(limit + 1).ToListAsync();
        var hasNextPage = answers.Count > limit;
        var items = answers.Take(limit).ToList();

        var dtos = items.Select(a => MapToDto(a)).ToList();

        return new CursorPagedResult<AnswerResponseDto>
        {
            Items = dtos,
            PageInfo = new PageInfo
            {
                HasNextPage = hasNextPage,
                EndCursor = items.Any() ? CursorHelper.Encode(items.Last().CreatedAt, items.Last().AnswerId) : null
            }
        };
    }

    public async Task<string?> GetAnswerCodeAsync(int id)
    {
        return await _context.Answers
            .Where(a => a.AnswerId == id)
            .Select(a => a.CodeContent)
            .FirstOrDefaultAsync();
    }

    public async Task<AnswerResponseDto> CreateAnswerAsync(int questionId, CreateAnswerDto dto, int userId)
    {
        var lineCount = string.IsNullOrEmpty(dto.CodeContent) ? 0 : dto.CodeContent.Split('\n').Length;
        var answer = new Answer
        {
            QuestionId = questionId,
            UserId = userId,
            Content = dto.Content,
            CodeContent = dto.CodeContent,
            CodeLanguage = dto.CodeLanguage,
            CodeLineCount = lineCount,
            ParentId = dto.ParentId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Answers.Add(answer);
        await _context.SaveChangesAsync();

        return MapToDto(await _context.Answers
            .Include(a => a.User)
            .Include(a => a.Votes)
            .FirstAsync(a => a.AnswerId == answer.AnswerId));
    }

    public async Task<AnswerResponseDto?> UpdateAnswerAsync(int id, UpdateAnswerDto dto, int userId)
    {
        var answer = await _context.Answers
            .Include(a => a.User)
            .Include(a => a.Votes)
            .FirstOrDefaultAsync(a => a.AnswerId == id);

        if (answer == null || answer.UserId != userId)
        {
            return null;
        }

        answer.Content = dto.Content;
        if (dto.CodeContent != null)
        {
            answer.CodeContent = dto.CodeContent;
            answer.CodeLineCount = dto.CodeContent.Split('\n').Length;
        }
        if (dto.CodeLanguage != null)
            answer.CodeLanguage = dto.CodeLanguage;
        answer.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return MapToDto(answer);
    }

    public async Task<bool> DeleteAnswerAsync(int id, int userId)
    {
        var answer = await _context.Answers.FirstOrDefaultAsync(a => a.AnswerId == id);

        if (answer == null || answer.UserId != userId)
        {
            return false;
        }

        _context.Answers.Remove(answer);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AcceptAnswerAsync(int id, int userId)
    {
        var answer = await _context.Answers
            .Include(a => a.Question)
            .FirstOrDefaultAsync(a => a.AnswerId == id);

        if (answer == null || answer.Question.UserId != userId)
        {
            return false;
        }

        // Unaccept other answers
        var otherAnswers = await _context.Answers
            .Where(a => a.QuestionId == answer.QuestionId && a.AnswerId != id)
            .ToListAsync();

        foreach (var other in otherAnswers)
        {
            other.IsAccepted = false;
        }

        answer.IsAccepted = true;
        await _context.SaveChangesAsync();
        return true;
    }

    private AnswerResponseDto MapToDto(Answer a)
    {
        var shouldIncludeCode = a.CodeLineCount < 20;
        return new AnswerResponseDto
        {
            AnswerId = a.AnswerId,
            QuestionId = a.QuestionId,
            Content = a.Content,
            CodeContent = shouldIncludeCode ? a.CodeContent : null,
            CodeLanguage = a.CodeLanguage,
            CodeLineCount = a.CodeLineCount,
            IsAccepted = a.IsAccepted,
            CreatedAt = a.CreatedAt,
            UpdatedAt = a.UpdatedAt,
            ParentId = a.ParentId,
            UserId = a.UserId,
            Username = a.User.Username,
            AvatarUrl = a.User.AvatarUrl,
            VoteCount = a.Votes.Sum(v => v.VoteType)
        };
    }
}
