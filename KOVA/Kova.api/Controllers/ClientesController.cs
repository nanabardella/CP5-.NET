using Kova.Application.DTOs;
using Kova.Application.Interfaces.Repositories;
using Kova.Domain.Entities;
using Kova.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Kova.api.Controllers;

[Asp.Versioning.ApiVersionNeutral]
[ApiController]
[Route("api/clientes")]
public sealed class ClientesController(IClienteRepository repository) : ControllerBase
{
    /// <summary>Lista todos os clientes.</summary>
    /// <response code="200">Clientes encontrados.</response>
    /// <response code="500">Erro interno ao consultar os clientes.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<ClienteResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public ActionResult<IReadOnlyCollection<ClienteResponse>> GetAll()
    {
        var response = repository.GetAll()
            .Select(ToResponse)
            .ToArray();

        return Ok(response);
    }

    /// <summary>Busca um cliente pelo identificador.</summary>
    /// <response code="200">Cliente encontrado.</response>
    /// <response code="404">Cliente não encontrado.</response>
    /// <response code="500">Erro interno ao consultar o cliente.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ClienteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public ActionResult<ClienteResponse> GetById(Guid id)
    {
        var cliente = repository.GetById(id)
            ?? throw new ResourceNotFoundException("Cliente", id);

        return Ok(ToResponse(cliente));
    }

    /// <summary>Cria um cliente.</summary>
    /// <response code="201">Cliente criado.</response>
    /// <response code="400">Payload inválido ou regra de domínio violada.</response>
    /// <response code="500">Erro interno ao criar o cliente.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ClienteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public ActionResult<ClienteResponse> Create(ClienteRequest request)
    {
        var cliente = new Cliente(request.Nome, request.Email, request.Telefone);
        repository.Add(cliente);
        repository.SaveChanges();

        return CreatedAtAction(nameof(GetById), new { id = cliente.Id }, ToResponse(cliente));
    }

    private static ClienteResponse ToResponse(Cliente cliente) =>
        new(cliente.Id, cliente.Nome, cliente.Email, cliente.Telefone, cliente.Active);
}
