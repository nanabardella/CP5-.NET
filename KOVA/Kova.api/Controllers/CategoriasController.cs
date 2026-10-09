using Kova.Application.DTOs;
using Kova.Application.Interfaces.Repositories;
using Kova.Domain.Entities;
using Kova.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Kova.api.Controllers;

[Asp.Versioning.ApiVersionNeutral]
[ApiController]
[Route("api/categorias")]
public sealed class CategoriasController(IRepository<Categoria> repository) : ControllerBase
{
    /// <summary>Lista todas as categorias.</summary>
    /// <response code="200">Categorias encontradas.</response>
    /// <response code="500">Erro interno ao consultar as categorias.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<CategoriaResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public ActionResult<IReadOnlyCollection<CategoriaResponse>> GetAll()
    {
        var response = repository.GetAll()
            .Select(ToResponse)
            .ToArray();

        return Ok(response);
    }

    /// <summary>Busca uma categoria pelo identificador.</summary>
    /// <response code="200">Categoria encontrada.</response>
    /// <response code="404">Categoria não encontrada.</response>
    /// <response code="500">Erro interno ao consultar a categoria.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CategoriaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public ActionResult<CategoriaResponse> GetById(Guid id)
    {
        var categoria = repository.GetById(id)
            ?? throw new ResourceNotFoundException("Categoria", id);

        return Ok(ToResponse(categoria));
    }

    /// <summary>Cria uma categoria.</summary>
    /// <remarks>A categoria criada é persistida no banco de dados.</remarks>
    /// <response code="201">Categoria criada.</response>
    /// <response code="400">Payload inválido ou regra de domínio violada.</response>
    /// <response code="500">Erro interno ao criar a categoria.</response>
    [HttpPost]
    [ProducesResponseType(typeof(CategoriaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public ActionResult<CategoriaResponse> Create(CategoriaRequest request)
    {
        var categoria = new Categoria(request.Nome, request.Descricao);
        repository.Add(categoria);
        repository.SaveChanges();

        return CreatedAtAction(nameof(GetById), new { id = categoria.Id }, ToResponse(categoria));
    }

    private static CategoriaResponse ToResponse(Categoria categoria) =>
        new(categoria.Id, categoria.Nome, categoria.Descricao, categoria.Active);
}
