using Asp.Versioning;
using Microsoft.AspNetCore.RateLimiting;
using Kova.Application.DTOs;
using Kova.Application.Interfaces.Repositories;
using Kova.Application.Interfaces.Services;
using Kova.Domain.Entities;
using Kova.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Kova.api.Controllers;

[ApiController]
[ApiVersion("1.0", Deprecated = true)]
[ApiVersion("2.0")]
[Route("api/produtos")]
public sealed class ProdutosController(
    IProdutoRepository repository,
    IProdutoService service,
    ILogger<ProdutosController> logger) : ControllerBase
{
    /// <summary>Lista todos os produtos.</summary>
    /// <response code="200">Produtos encontrados.</response>
    /// <response code="500">Erro interno ao consultar os produtos.</response>
    [HttpGet]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ProdutoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public ActionResult<IReadOnlyCollection<ProdutoResponse>> GetAll()
    {
        var response = service.GetAll();

        return Ok(response);
    }

    /// <summary>Lista produtos com paginação (v2).</summary>
    [HttpGet]
    [MapToApiVersion("2.0")]
    [ProducesResponseType(typeof(PagedResponse<ProdutoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<PagedResponse<ProdutoResponse>> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => Ok(service.GetPaged(page, pageSize));

    /// <summary>Busca um produto pelo identificador.</summary>
    /// <response code="200">Produto encontrado.</response>
    /// <response code="404">Produto não encontrado.</response>
    /// <response code="500">Erro interno ao consultar o produto.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProdutoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public ActionResult<ProdutoResponse> GetById(Guid id)
    {
        var produto = repository.GetById(id)
            ?? throw new ResourceNotFoundException("Produto", id);

        return Ok(ToResponse(produto));
    }

    /// <summary>Cria um produto associado a uma categoria existente.</summary>
    /// <response code="201">Produto criado.</response>
    /// <response code="400">Payload inválido ou regra de domínio violada.</response>
    /// <response code="404">Categoria não encontrada.</response>
    /// <response code="500">Erro interno ao criar o produto.</response>
    [HttpPost]
    [EnableRateLimiting("produto-write")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProdutoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public ActionResult<ProdutoResponse> Create(ProdutoRequest request)
    {
        var traceId = HttpContext.TraceIdentifier;
        logger.LogInformation(
            "Starting product creation for {CategoriaId}. TraceId: {TraceId}",
            request.CategoriaId, traceId);

        var response = service.Create(request);

        logger.LogInformation(
            "Product {ProdutoId} created successfully. TraceId: {TraceId}",
            response.Id, traceId);

        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    private static ProdutoResponse ToResponse(Produto produto) =>
        new(produto.Id, produto.Nome, produto.Descricao, produto.Preco, produto.CategoriaId, produto.Active);
}
