using Kova.Application.DTOs;

namespace Kova.Application.Interfaces.Services;

public interface IProdutoService
{
    IReadOnlyCollection<ProdutoResponse> GetAll();
    PagedResponse<ProdutoResponse> GetPaged(int page = 1, int pageSize = 20);
    ProdutoResponse Create(ProdutoRequest request);
}
