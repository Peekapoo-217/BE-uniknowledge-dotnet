using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using UniKnowledge.Models.Indexes;
using UniKnowledge.Services;
using UniKnowledge.Data;
using UniKnowledge.DTOs.Question;
using UniKnowledge.DTOs.Search;
using UniKnowledge.Constants;

namespace UniKnowledge.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;
    private readonly IQuestionService _questionService;
    private readonly AppDbContext _context;

    public SearchController(ISearchService searchService, IQuestionService questionService, AppDbContext context)
    {
        _searchService = searchService;
        _questionService = questionService;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q)) return BadRequest("Query is required");
        
        var results = await _searchService.SearchQuestionsAsync(q);
        var questions = await GetFullQuestionsFromIds(results.Select(r => r.Id).ToList());
        return Ok(questions);
    }

    [HttpPost("advanced")]
    public async Task<IActionResult> AdvancedSearch([FromBody] SearchFilterDto filter)
    {
        var results = await _searchService.AdvancedSearchAsync(filter);
        var questions = await GetFullQuestionsFromIds(results.Select(r => r.Id).ToList());
        return Ok(questions);
    }

    [HttpPost("index")]
    public async Task<IActionResult> Index([FromBody] QuestionIndexDocument doc)
    {
        var result = await _searchService.IndexQuestionAsync(doc);
        return result ? Ok("Indexed successfully") : StatusCode(500, "Failed to index");
    }

    [HttpPost("reindex-all")]
    public async Task<IActionResult> ReindexAll()
    {
        try
        {
            var questions = await _context.Questions
                .Include(q => q.QuestionTags)
                    .ThenInclude(qt => qt.Tag)
                .Include(q => q.Votes)
                .ToListAsync();

            var docs = questions.Select(q => new QuestionIndexDocument
            {
                Id = q.QuestionId,
                Title = q.Title,
                Content = q.Content ?? string.Empty,
                Tags = q.QuestionTags?.Select(qt => qt.Tag?.TagName ?? "").Where(t => !string.IsNullOrEmpty(t)).ToList() ?? new List<string>(),
                Upvotes = q.Votes?.Count(v => v.VoteType == 1) ?? 0 - (q.Votes?.Count(v => v.VoteType == -1) ?? 0),
                IsSolved = q.Status == QuestionStatus.Closed,
                CategoryId = q.CategoryId
            }).ToList();

            if (!docs.Any())
            {
                return Ok("No questions found in database to index.");
            }

            var result = await _searchService.BulkIndexAsync(docs);
            return result ? Ok($"Reindexed {docs.Count} questions") : StatusCode(500, "Failed to reindex");
        }
        catch (System.Exception ex)
        {
            return StatusCode(500, $"Internal error: {ex.Message}");
        }
    }

    private async Task<List<QuestionSummaryDto>> GetFullQuestionsFromIds(List<int> ids)
    {
        if (!ids.Any()) return new List<QuestionSummaryDto>();

        var questions = await _context.Questions
            .Include(q => q.User)
            .Include(q => q.Category)
            .Include(q => q.QuestionTags)
                .ThenInclude(qt => qt.Tag)
            .Include(q => q.Answers)
            .Include(q => q.Votes)
            .Where(q => ids.Contains(q.QuestionId))
            .ToListAsync();

        // Map and Sort back to follow the order from Elasticsearch (scores)
        var questionService = (QuestionService)_questionService;
        return ids
            .Select(id => questions.FirstOrDefault(q => q.QuestionId == id))
            .Where(q => q != null)
            .Select(q => questionService.MapToSummaryDto(q!))
            .ToList();
    }
}
