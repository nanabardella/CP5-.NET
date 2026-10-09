using Kova.Application.Interfaces.Repositories;
using Kova.Domain.Commons;
using Kova.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kova.Infrastructure.Repositories;

public class Repository<T>(KovaDbContext context) : IRepository<T> where T : BaseEntity
{
    private readonly DbSet<T> entities = context.Set<T>();

    public IReadOnlyCollection<T> GetAll() => entities.AsNoTracking().ToList();

    public Kova.Application.DTOs.PagedResponse<T> GetPaged(Kova.Application.DTOs.PageRequest request)
    {
        var query = entities.AsNoTracking();
        var total = query.Count();
        var offset = ((long)request.Page - 1) * request.PageSize;
        IReadOnlyCollection<T> items = offset >= total ? Array.Empty<T>() : query
            .OrderBy(entity => entity.CreatedAt).ThenBy(entity => entity.Id)
            .Skip((int)offset).Take(request.PageSize).ToList();
        return new(request.Page, request.PageSize, total,
            (int)Math.Ceiling((double)total / request.PageSize), items);
    }
    public T? GetById(Guid id) => entities.AsNoTracking().FirstOrDefault(entity => entity.Id == id);

    public bool ExistsById(Guid id) => entities.Any(entity => entity.Id == id);

    public void Add(T entity) => entities.Add(entity);

    public void Update(T entity) => entities.Update(entity);

    public void Delete(T entity) => entities.Remove(entity);

    public void SaveChanges() => context.SaveChanges();
}
