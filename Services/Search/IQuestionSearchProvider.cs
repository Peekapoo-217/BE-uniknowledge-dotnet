using System.Collections.Generic;
using System.Threading.Tasks;
using UniKnowledge.Models.Indexes;
using UniKnowledge.DTOs.Search;

namespace UniKnowledge.Services.Search;

public interface IQuestionSearchProvider
{
    string ProviderName { get; }
    Task<List<QuestionIndexDocument>> SearchAsync(string keyword);
    Task<List<QuestionIndexDocument>> AdvancedSearchAsync(SearchFilterDto filter);
}
