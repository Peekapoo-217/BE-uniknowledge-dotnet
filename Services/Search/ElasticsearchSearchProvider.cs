using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UniKnowledge.Models.Indexes;
using UniKnowledge.DTOs.Search;

namespace UniKnowledge.Services.Search;

public class ElasticsearchSearchProvider : IQuestionSearchProvider
{
    private readonly ElasticsearchClient _client;
    private const string IndexName = "questions_index";

    public string ProviderName => "Elasticsearch";

    public ElasticsearchSearchProvider(ElasticsearchClient client)
    {
        _client = client;
    }

    public async Task<List<QuestionIndexDocument>> SearchAsync(string keyword)
    {
        var response = await _client.SearchAsync<QuestionIndexDocument>(s => s
            .Index(IndexName)
            .Query(q => q
                .MultiMatch(m => m
                    .Fields(new[] { "title", "content" })
                    .Query(keyword)
                    .Fuzziness(new Fuzziness("AUTO"))
                )
            )
        );

        return response.IsValidResponse ? response.Documents.ToList() : new List<QuestionIndexDocument>();
    }

    public async Task<List<QuestionIndexDocument>> AdvancedSearchAsync(SearchFilterDto filter)
    {
        var response = await _client.SearchAsync<QuestionIndexDocument>(s => s
            .Index(IndexName)
            .Query(q => q
                .Bool(b => b
                    .Must(m => {
                        if (!string.IsNullOrWhiteSpace(filter.Keyword))
                        {
                            m.MultiMatch(mm => mm
                                .Fields(new[] { "title", "content" })
                                .Query(filter.Keyword)
                                .Fuzziness(new Fuzziness("AUTO"))
                            );
                        }
                    })
                    .Filter(f => {
                        if (filter.CategoryId.HasValue)
                        {
                            f.Term(t => t.Field(ff => ff.CategoryId).Value(filter.CategoryId.Value));
                        }
                        if (!string.IsNullOrWhiteSpace(filter.Tag))
                        {
                            f.Term(t => t.Field(ff => ff.Tags).Value(filter.Tag));
                        }
                        if (filter.IsSolved.HasValue)
                        {
                            f.Term(t => t.Field(ff => ff.IsSolved).Value(filter.IsSolved.Value));
                        }
                    })
                )
            )
        );

        return response.IsValidResponse ? response.Documents.ToList() : new List<QuestionIndexDocument>();
    }
}
