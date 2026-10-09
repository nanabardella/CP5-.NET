using Kova.Application.DTOs;
using Kova.Application.Interfaces.Repositories;
using Kova.Application.Interfaces.Services;
using Kova.Domain.Entities;
using Kova.Domain.Exceptions;

namespace Kova.Application.Services;

public sealed class ProdutoService(
    IRepository<Produto> produtoRepository,
    IRepository<Categoria> categoriaRepository) : IProdutoService
{
    public IReadOnlyCollection<ProdutoResponse> GetAll() => produtoRepository.GetAll().Select(ToResponse).ToArray();
    public PagedResponse<ProdutoResponse> GetPaged(int page = 1, int pageSize = 20)
    {
        var result = produtoRepository.GetPaged(new PageRequest(page, pageSize));
        return new(result.Page, result.PageSize, result.TotalItems, result.TotalPages, result.Items.Select(ToResponse).ToArray());
    }
    private static ProdutoResponse ToResponse(Produto produto) =>
        new(produto.Id, produto.Nome, produto.Descricao, produto.Preco, produto.CategoriaId, produto.Active);
    public ProdutoResponse Create(ProdutoRequest request)
    {
        if (!categoriaRepository.ExistsById(request.CategoriaId))
            throw new ResourceNotFoundException("Categoria", request.CategoriaId);

        var produto = new Produto(request.Nome, request.Descricao, request.Preco, request.CategoriaId);
        produtoRepository.Add(produto);
        produtoRepository.SaveChanges();

        return new ProdutoResponse(
            produto.Id,
            produto.Nome,
            produto.Descricao,
            produto.Preco,
            produto.CategoriaId,
            produto.Active);
    }
}
