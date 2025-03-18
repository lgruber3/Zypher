using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Model;


namespace Domain.Repositories;


public abstract class ARepositoryAsync<TEntity> : IRepository<TEntity> where TEntity : class
{
    protected readonly ContextClass _context;
    protected readonly DbSet<TEntity> _dbSet;

    protected ARepositoryAsync(ContextClass context)
    {
        _context = context;
        _dbSet = context.Set<TEntity>(); 
    }
    
    public async Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {  
        var entity = await ReadAsync(id, cancellationToken);
        if (entity is not null)
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }throw new NotImplementedException();
    }

    public async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<TEntity?> ReadAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<List<TEntity>> ReadAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(filter).ToListAsync(cancellationToken);
    }

    public async Task<List<TEntity>> ReadAsync(int start, int count, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Skip(start).Take(count).ToListAsync(cancellationToken);
    }

    public async Task<List<TEntity>> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }
}

    
