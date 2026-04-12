using System.Collections.Generic;
using System.Threading.Tasks;
using UniKnowledge.Models.Indexes;

namespace UniKnowledge.Services;

public interface ISearchService
{
    Task<bool> IndexQuestionAsync(QuestionIndexDocument document);
    Task<bool> DeleteQuestionAsync(int id);
    Task<bool> BulkIndexAsync(IEnumerable<QuestionIndexDocument> documents);
    Task<List<QuestionIndexDocument>> SearchQuestionsAsync(string keyword);
    Task<List<QuestionIndexDocument>> AdvancedSearchAsync(UniKnowledge.DTOs.Search.SearchFilterDto filter);
}
