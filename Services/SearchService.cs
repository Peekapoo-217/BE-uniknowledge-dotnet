using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UniKnowledge.Models.Indexes;
using UniKnowledge.DTOs.Search;
using UniKnowledge.Services.Search;
using Elastic.Clients.Elasticsearch;

namespace UniKnowledge.Services;

public class SearchService : ISearchService
{
    private readonly ElasticsearchSearchProvider _esProvider;
    private readonly SqlSearchProvider _sqlProvider;
    private readonly ElasticsearchClient _client; // Keep for low-level tasks if needed, but primarily use providers
    private const string IndexName = "questions_index";

    public SearchService(ElasticsearchSearchProvider esProvider, SqlSearchProvider sqlProvider, ElasticsearchClient client)
    {
        _esProvider = esProvider;
        _sqlProvider = sqlProvider;
        _client = client;
    }

    public async Task<bool> IndexQuestionAsync(QuestionIndexDocument document)
    {
        try
        {
            var response = await _client.IndexAsync(document, idx => idx.Index(IndexName));
            return response.IsValidResponse;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Elasticsearch Indexing Error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteQuestionAsync(int id)
    {
        try
        {
            var response = await _client.DeleteAsync<QuestionIndexDocument>(id, idx => idx.Index(IndexName));
            return response.IsValidResponse;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Elasticsearch Deletion Error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> BulkIndexAsync(IEnumerable<QuestionIndexDocument> documents)
    {
        if (documents == null || !documents.Any()) return true;

        try
        {
            var response = await _client.BulkAsync(b => b.Index(IndexName).IndexMany(documents));
            return response.IsValidResponse;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Elasticsearch Bulk Indexing Error: {ex.Message}");
            return false;
        }
    }

    public async Task<List<QuestionIndexDocument>> SearchQuestionsAsync(string keyword)
    {
        return await ExecuteWithFallback(p => p.SearchAsync(keyword));
    }

    public async Task<List<QuestionIndexDocument>> AdvancedSearchAsync(SearchFilterDto filter)
    {
        return await ExecuteWithFallback(p => p.AdvancedSearchAsync(filter));
    }

    private async Task<List<QuestionIndexDocument>> ExecuteWithFallback(Func<IQuestionSearchProvider, Task<List<QuestionIndexDocument>>> action)
    {
        try
        {
            // Try Elasticsearch first
            var results = await action(_esProvider);
            if (results.Any())
            {
                return results;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Elasticsearch search failed, falling back to SQL: {ex.Message}");
        }

        // Fallback to SQL
        return await action(_sqlProvider);
    }
}
