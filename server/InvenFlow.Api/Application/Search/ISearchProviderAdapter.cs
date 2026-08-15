using InvenFlow.Api.Application.DTOs;

namespace InvenFlow.Api.Application.Search;

public interface ISearchProviderAdapter
{
    string CategoryDomain { get; }
    Task<List<UniversalProductDto>> SearchAsync(string query);
}
