using Kova.Domain.Commons;

namespace Kova.Application.Interfaces.Repositories;

public interface IRepository<T> where T : BaseEntity
{
    IReadOnlyCollection<T> GetAll();
    Kova.Application.DTOs.PagedResponse<T> GetPaged(Kova.Application.DTOs.PageRequest request);
    T? GetById(Guid id);
    bool ExistsById(Guid id);
    void Add(T entity);
    void Update(T entity);
    void Delete(T entity);
    void SaveChanges();
}
