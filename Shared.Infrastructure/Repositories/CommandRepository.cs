using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Shared.Application.Common;
using Shared.Application.Interfaces;
using Shared.Application.Interfaces.Repositories;
using Shared.Infrastructure.Contexts;

namespace Shared.Infrastructure.Repositories;

public class CommandRepository<TEntity>(AppDbContext context) : ICommandRepository<TEntity> where TEntity : class
{
    private DbSet<TEntity> DbSet => context.Set<TEntity>();

    public IQueryable<TEntity> AsQueryable(Expression<Func<TEntity, bool>>? predicate = null)
    {
        return predicate == null ? DbSet.AsQueryable() : DbSet.Where(predicate);
    }

    /// <summary>
    /// Find a record
    /// </summary>
    /// <param name="predicate"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        if (predicate != null)
            return await DbSet.FirstOrDefaultAsync(predicate, cancellationToken);
    
        return await DbSet.FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Find all records
    /// </summary>
    /// <param name="predicate"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IEnumerable<TEntity>> ToListAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        if (predicate != null)
            return await DbSet.Where(predicate).ToListAsync(cancellationToken);

        return await DbSet.ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get paged records
    /// </summary>
    /// <param name="pageNumber"></param>
    /// <param name="pageSize"></param>
    /// <param name="predicate"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<PagedResult<TEntity>> PagedAsync(int pageNumber, int pageSize, Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        var query = predicate != null ? DbSet.Where(predicate) : DbSet;
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
    
        return new PagedResult<TEntity>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
    
    /// <summary>
    /// Check if an entity exists based on the provided predicate.
    /// </summary>
    /// <param name="predicate"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(predicate, cancellationToken);
    }
    
    /// <summary>
    /// Count entities based on the provided predicate.
    /// </summary>
    /// <param name="predicate"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        if (predicate != null)
            return await DbSet.CountAsync(predicate, cancellationToken);
    
        return await DbSet.CountAsync(cancellationToken);
    }

    /// <summary>
    /// Add entity to the database
    /// </summary>
    /// <param name="entity"></param>
    public async Task AddAsync(TEntity entity)
    {
        await context.AddAsync(entity);
    }

    /// <summary>
    /// Add a range of entities to the database asynchronously
    /// </summary>
    /// <param name="entities"></param>
    public async Task AddRangeAsync(IEnumerable<TEntity> entities)
    {
        await context.AddRangeAsync(entities);
    }

    /// <summary>
    /// Update entity in the database
    /// </summary>
    /// <param name="entity"></param>
    public void Update(TEntity entity)
    {
        DbSet.Update(entity);
    }

    /// <summary>
    /// Update a range of entities in the database
    /// </summary>
    /// <param name="entities"></param>
    public void UpdateRange(IEnumerable<TEntity> entities)
    {
        DbSet.UpdateRange(entities);
    }
}