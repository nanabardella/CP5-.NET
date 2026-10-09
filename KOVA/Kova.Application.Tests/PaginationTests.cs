using System;
using Kova.Application.DTOs;
using Kova.Application.Interfaces.Repositories;
using Kova.Application.Services;
using Kova.Domain.Entities;
using Moq;
using Xunit;
namespace Kova.Application.Tests;
public sealed class PaginationTests
{
    [Theory]
    [InlineData(0, 20)]
    [InlineData(-1, 20)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    [InlineData(1, 101)]
    [InlineData(1, 9999)]
    public void InvalidPageDoesNotQueryRepository(int page, int size)
    {
        var repository = new Mock<IRepository<Produto>>(MockBehavior.Strict);
        var service = new ProdutoService(repository.Object, Mock.Of<IRepository<Categoria>>());
        Assert.Throws<ArgumentException>(() => service.GetPaged(page, size));
        repository.VerifyNoOtherCalls();
    }
    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 20)]
    [InlineData(int.MaxValue, 100)]
    public void ValidPagePreservesTotalsAndMapsItems(int page, int size)
    {
        var repository = new Mock<IRepository<Produto>>();
        var product = new Produto("Camiseta", "Camiseta de algodão", 50m, Guid.NewGuid());
        repository.Setup(r => r.GetPaged(It.Is<PageRequest>(p => p.Page == page && p.PageSize == size)))
            .Returns(new PagedResponse<Produto>(page, size, 137, (int)Math.Ceiling(137d / size), new[] { product }));
        var result = new ProdutoService(repository.Object, Mock.Of<IRepository<Categoria>>()).GetPaged(page, size);
        Assert.Equal(137, result.TotalItems);
        Assert.Equal((int)Math.Ceiling(137d / size), result.TotalPages);
        Assert.Equal(product.Id, Assert.Single(result.Items).Id);
    }
}
