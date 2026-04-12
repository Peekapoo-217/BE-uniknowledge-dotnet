using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UniKnowledge.Data;
using UniKnowledge.Models.Indexes;
using UniKnowledge.DTOs.Search;
using UniKnowledge.Constants;

namespace UniKnowledge.Services.Search;

public class SqlSearchProvider : IQuestionSearchProvider
{
    private readonly AppDbContext _context;

    public string ProviderName => "SQL";

    public SqlSearchProvider(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<QuestionIndexDocument>> SearchAsync(string keyword)
    {
        return await AdvancedSearchAsync(new SearchFilterDto(keyword, null, null, null));
    }

    public async Task<List<QuestionIndexDocument>> AdvancedSearchAsync(SearchFilterDto filter)
    {
        var query = _context.Questions
            .Include(q => q.QuestionTags)
                .ThenInclude(qt => qt.Tag)
            .Include(q => q.Votes)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var k = filter.Keyword.ToLower();
            query = query.Where(q => q.Title.ToLower().Contains(k) || (q.Content != null && q.Content.ToLower().Contains(k)));
        }

        if (filter.CategoryId.HasValue)
        {
            query = query.Where(q => q.CategoryId == filter.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Tag))
        {
            query = query.Where(q => q.QuestionTags.Any(qt => qt.Tag.TagName == filter.Tag));
        }

        if (filter.IsSolved.HasValue)
        {
            var status = filter.IsSolved.Value ? QuestionStatus.Closed : QuestionStatus.Open;
            query = query.Where(q => q.Status == status);
        }

        var results = await query.ToListAsync();

        return results.Select(q => new QuestionIndexDocument
        {
            Id = q.QuestionId,
            Title = q.Title,
            Content = q.Content ?? string.Empty,
            Tags = q.QuestionTags.Select(qt => qt.Tag.TagName).ToList(),
            Upvotes = q.Votes.Sum(v => v.VoteType),
            IsSolved = q.Status == QuestionStatus.Closed,
            CategoryId = q.CategoryId
        }).ToList();
    }
}
