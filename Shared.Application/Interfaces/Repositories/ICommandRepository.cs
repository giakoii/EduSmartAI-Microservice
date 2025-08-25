using System.Linq.Expressions;
using Shared.Application.Common;

namespace Shared.Application.Interfaces.Repositories;

public interface ICommandRepository<TEntity> where TEntity : class
{
    /// <summary>
    /// Get IQueryable for the entity.
    /// </summary>
    /// <param name="predicate"></param>
    /// <returns></returns>
    IQueryable<TEntity> AsQueryable(Expression<Func<TEntity, bool>>? predicate = null);
    
    /// <summary>
    /// Get first entity matching the predicate.
    /// </summary>
    Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all entities matching the predicate.
    /// </summary>
    Task<IEnumerable<TEntity>> ToListAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get paged entities.
    /// </summary>
    Task<PagedResult<TEntity>> PagedAsync(int pageNumber, int pageSize, Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if entity exists.
    /// </summary>
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Count entities matching the predicate.
    /// </summary>
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Add entity to the database.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    Task AddAsync(TEntity entity);
    
    /// <summary>
    /// Add a range of entities to the database asynchronously.
    /// </summary>
    /// <param name="entities"></param>
    /// <returns></returns>
    Task AddRangeAsync(IEnumerable<TEntity> entities);

    /// <summary>
    /// Update entity in the database.
    /// </summary>
    /// <param name="entity"></param>
    void Update(TEntity entity);
    
    /// <summary>
    /// Update a range of entities in the database.
    /// </summary>
    /// <param name="entities"></param>
    void UpdateRange(IEnumerable<TEntity> entities);
}