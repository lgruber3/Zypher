using System.Linq.Expressions;

namespace Domain;

public interface IRepository<TEntity> where TEntity : class
{
    Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken = default);
    
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    
    Task<TEntity?> ReadAsync(int id, CancellationToken cancellationToken = default);
    
    Task<List<TEntity>> ReadAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default);
    
    Task<List<TEntity>> ReadAsync(int start, int count, CancellationToken cancellationToken = default);
    
    Task<List<TEntity>> ReadAllAsync(CancellationToken cancellationToken = default);
}