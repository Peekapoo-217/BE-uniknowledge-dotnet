using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UniKnowledge.Data;
using UniKnowledge.DTOs.Shared;
using UniKnowledge.DTOs.Tag;
using UniKnowledge.DTOs.Question;
using UniKnowledge.Helpers;
using UniKnowledge.Models;

namespace UniKnowledge.Services;

public class TagService : ITagService
{
    private readonly AppDbContext _context;

    public TagService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(bool Success, string Message, int Count, object? Tags)> SeedTagsAsync()
    {
        // Double check if tags already exist and clear them (original logic)
        if (await _context.Tags.AnyAsync())
        {
            var existingTags = await _context.Tags.ToListAsync();
            _context.Tags.RemoveRange(existingTags);
            await _context.SaveChangesAsync();
        }

        var tags = new List<Tag>
        {
            new Tag { TagName = "C#", Description = "C# programming language" },
            new Tag { TagName = "Reactjs", Description = "React JavaScript library" },
            new Tag { TagName = "NodeJs", Description = "Node.js runtime environment" }
        };

        _context.Tags.AddRange(tags);
        await _context.SaveChangesAsync();

        var tagsResult = tags.Select(t => new
        {
            t.TagId,
            t.TagName,
            t.Description
        });

        return (true, "Tags seeded successfully", tags.Count, tagsResult);
    }

    public async Task<(bool Success, string Message, TagResponseDto? Tag)> CreateTagAsync(CreateTagDto dto)
    {
        if (await _context.Tags.AnyAsync(t => t.TagName == dto.TagName))
        {
            return (false, "Tag name already exists", null);
        }

        var tag = new Tag
        {
            TagName = dto.TagName,
            Description = dto.Description
        };

        _context.Tags.Add(tag);
        await _context.SaveChangesAsync();

        return (true, string.Empty, new TagResponseDto
        {
            TagId = tag.TagId,
            TagName = tag.TagName,
            Description = tag.Description,
            QuestionCount = 0
        });
    }

    public async Task<List<TagResponseDto>> GetTagsAsync(string? search)
    {
        var query = _context.Tags
            .Include(t => t.QuestionTags)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(t => t.TagName.Contains(search));
        }

        var tags = await query
            .OrderBy(t => t.TagName)
            .ToListAsync();

        return tags.Select(t => new TagResponseDto
        {
            TagId = t.TagId,
            TagName = t.TagName,
            Description = t.Description,
            QuestionCount = t.QuestionTags.Count
        }).ToList();
    }

    public async Task<TagResponseDto?> GetTagByIdAsync(int id)
    {
        var tag = await _context.Tags
            .Include(t => t.QuestionTags)
            .FirstOrDefaultAsync(t => t.TagId == id);

        if (tag == null) return null;

        return new TagResponseDto
        {
            TagId = tag.TagId,
            TagName = tag.TagName,
            Description = tag.Description,
            QuestionCount = tag.QuestionTags.Count
        };
    }

    public async Task<List<TagResponseDto>> GetPopularTagsAsync(int limit)
    {
        var tags = await _context.Tags
            .Include(t => t.QuestionTags)
            .OrderByDescending(t => t.QuestionTags.Count)
            .Take(limit)
            .ToListAsync();

        return tags.Select(t => new TagResponseDto
        {
            TagId = t.TagId,
            TagName = t.TagName,
            Description = t.Description,
            QuestionCount = t.QuestionTags.Count
        }).ToList();
    }

    private IQueryable<Question> ApplyCursorPagination(IQueryable<Question> query, string? after)
    {
        var cursor = CursorHelper.Decode(after);
        if (cursor.HasValue)
        {
            var (cursorDate, cursorId) = cursor.Value;
            query = query.Where(q => q.CreatedAt < cursorDate ||
                        (q.CreatedAt == cursorDate && q.QuestionId < cursorId));
        }
        return query;
    }

    private QuestionSummaryDto MapToQuestionSummaryDto(Question q)
    {
        return new QuestionSummaryDto
        {
            QuestionId = q.QuestionId,
            Title = q.Title,
            Content = q.Content,
            ViewCount = q.ViewCount,
            Status = q.Status,
            CreatedAt = q.CreatedAt,
            User = new UserSummaryDto { Username = q.User.Username, AvatarUrl = q.User.AvatarUrl },
            Category = q.Category != null ? new CategorySummaryDto { CategoryId = q.Category.CategoryId, CategoryName = q.Category.CategoryName } : null,
            Tags = q.QuestionTags.Select(qt => new TagSummaryDto { TagId = qt.Tag.TagId, TagName = qt.Tag.TagName }).ToList(),
            AnswerCount = q.Answers.Count,
            VoteCount = q.Votes.Sum(v => v.VoteType)
        };
    }

    public async Task<(bool Success, string Message, string? TagName, List<QuestionSummaryDto>? Items, PageInfo? PageInfo)> GetQuestionsByTagAsync(int id, int limit, string? after)
    {
        var tag = await _context.Tags.FirstOrDefaultAsync(t => t.TagId == id);
        if (tag == null)
        {
            return (false, $"Tag with id {id} not found", null, null, null);
        }

        var query = _context.Questions
            .Include(q => q.User)
            .Include(q => q.Category)
            .Include(q => q.QuestionTags)
                .ThenInclude(qt => qt.Tag)
            .Include(q => q.Answers)
            .Include(q => q.Votes)
            .Where(q => q.QuestionTags.Any(qt => qt.TagId == id) && q.Status != "Hidden");

        query = ApplyCursorPagination(query, after);

        var questions = await query
            .OrderByDescending(q => q.CreatedAt)
            .ThenByDescending(q => q.QuestionId)
            .Take(limit + 1)
            .ToListAsync();

        var hasNextPage = questions.Count > limit;
        var items = questions.Take(limit).ToList();

        var result = items.Select(MapToQuestionSummaryDto).ToList();

        var pageInfo = new PageInfo
        {
            HasNextPage = hasNextPage,
            EndCursor = items.Any() ? CursorHelper.Encode(items.Last().CreatedAt, items.Last().QuestionId) : null
        };

        return (true, string.Empty, tag.TagName, result, pageInfo);
    }

    public async Task<List<TagResponseDto>> SuggestTagsAsync(string? query, int limit)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            return new List<TagResponseDto>();
        }

        var tags = await _context.Tags
            .Include(t => t.QuestionTags)
            .Where(t => t.TagName.Contains(query))
            .OrderByDescending(t => t.QuestionTags.Count)
            .ThenBy(t => t.TagName)
            .Take(limit)
            .ToListAsync();

        return tags.Select(t => new TagResponseDto
        {
            TagId = t.TagId,
            TagName = t.TagName,
            Description = t.Description,
            QuestionCount = t.QuestionTags.Count
        }).ToList();
    }

    public async Task<List<TagResponseDto>> GetTrendingTagsAsync(int days, int limit)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-days);
        
        var tags = await _context.Tags
            .Include(t => t.QuestionTags)
                .ThenInclude(qt => qt.Question)
            .Select(t => new
            {
                Tag = t,
                RecentQuestionCount = t.QuestionTags
                    .Count(qt => qt.Question.CreatedAt >= cutoffDate && qt.Question.Status != "Hidden"),
                TotalQuestionCount = t.QuestionTags.Count
            })
            .Where(x => x.RecentQuestionCount > 0)
            .OrderByDescending(x => x.RecentQuestionCount)
            .ThenByDescending(x => x.TotalQuestionCount)
            .Take(limit)
            .ToListAsync();

        return tags.Select(x => new TagResponseDto
        {
            TagId = x.Tag.TagId,
            TagName = x.Tag.TagName,
            Description = x.Tag.Description,
            QuestionCount = x.TotalQuestionCount
        }).ToList();
    }

    public async Task<(List<QuestionSummaryDto> Items, PageInfo PageInfo)> FilterQuestionsByTagsAsync(TagFilterDto dto)
    {
        var query = _context.Questions
            .Include(q => q.User)
            .Include(q => q.Category)
            .Include(q => q.QuestionTags)
                .ThenInclude(qt => qt.Tag)
            .Include(q => q.Answers)
            .Include(q => q.Votes)
            .AsQueryable();

        if (dto.TagIds != null && dto.TagIds.Any())
        {
            if (dto.Logic?.ToUpper() == "OR")
            {
                query = query.Where(q => q.QuestionTags.Any(qt => dto.TagIds.Contains(qt.TagId)));
            }
            else
            {
                query = query.Where(q => dto.TagIds.All(tagId => 
                    q.QuestionTags.Any(qt => qt.TagId == tagId)));
            }
        }

        query = query.Where(q => q.Status != "Hidden");
        query = ApplyCursorPagination(query, dto.After);

        var questions = await query
            .OrderByDescending(q => q.CreatedAt)
            .ThenByDescending(q => q.QuestionId)
            .Take(dto.Limit + 1)
            .ToListAsync();

        var hasNextPage = questions.Count > dto.Limit;
        var items = questions.Take(dto.Limit).ToList();

        var result = items.Select(MapToQuestionSummaryDto).ToList();

        var pageInfo = new PageInfo
        {
            HasNextPage = hasNextPage,
            EndCursor = items.Any() ? CursorHelper.Encode(items.Last().CreatedAt, items.Last().QuestionId) : null
        };

        return (result, pageInfo);
    }

    public async Task<(bool Success, string Message, TagResponseDto? Tag)> UpdateTagAsync(int id, UpdateTagDto dto)
    {
        var tag = await _context.Tags
            .Include(t => t.QuestionTags)
            .FirstOrDefaultAsync(t => t.TagId == id);

        if (tag == null)
        {
            return (false, "Tag not found", null);
        }

        if (!string.IsNullOrEmpty(dto.TagName) && dto.TagName != tag.TagName)
        {
            if (await _context.Tags.AnyAsync(t => t.TagName == dto.TagName && t.TagId != id))
            {
                return (false, "Tag name already exists", null);
            }
        }

        if (!string.IsNullOrEmpty(dto.TagName))
        {
            tag.TagName = dto.TagName;
        }

        if (dto.Description != null)
        {
            tag.Description = dto.Description;
        }

        await _context.SaveChangesAsync();

        return (true, string.Empty, new TagResponseDto
        {
            TagId = tag.TagId,
            TagName = tag.TagName,
            Description = tag.Description,
            QuestionCount = tag.QuestionTags.Count
        });
    }

    public async Task<(bool Success, string Message)> DeleteTagAsync(int id)
    {
        var tag = await _context.Tags
            .Include(t => t.QuestionTags)
            .FirstOrDefaultAsync(t => t.TagId == id);

        if (tag == null)
        {
            return (false, "Tag not found");
        }

        if (tag.QuestionTags.Any())
        {
            return (false, $"Cannot delete tag because it has {tag.QuestionTags.Count} question(s) associated with it");
        }

        _context.Tags.Remove(tag);
        await _context.SaveChangesAsync();

        return (true, string.Empty);
    }
}
