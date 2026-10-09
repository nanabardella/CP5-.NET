namespace Kova.Application.DTOs;
public sealed record PagedResponse<T>(int Page, int PageSize, int TotalItems, int TotalPages, IReadOnlyCollection<T> Items);
public sealed record PageRequest
{
    public int Page { get; }
    public int PageSize { get; }
    public PageRequest(int page = 1, int pageSize = 20)
    {
        if (page < 1) throw new ArgumentException("page deve ser maior ou igual a 1.", nameof(page));
        if (pageSize < 1 || pageSize > 100) throw new ArgumentException("pageSize deve estar entre 1 e 100.", nameof(pageSize));
        Page = page; PageSize = pageSize;
    }
}