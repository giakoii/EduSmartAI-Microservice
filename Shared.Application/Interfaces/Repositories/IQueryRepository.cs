using System.Linq.Expressions;

namespace Shared.Application.Interfaces.Repositories;

public interface IQueryRepository<TEntity> where TEntity : class
{
    /// <summary>
    /// Find entities by predicate as asynchronous
    /// </summary>
    Task<List<TEntity>> ToListAsync();

    /// <summary>
    /// Find entities by predicate as asynchronous
    /// </summary>
    /// <param name="predicate"></param>
    /// <returns></returns>
    Task<List<TEntity>> ToListAsync(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    /// Find entities by predicate
    /// </summary>
    /// <returns></returns>
    IEnumerable<TEntity> ToList();
    
    /// <summary>
    /// Find entity by predicate
    /// </summary>
    /// <param name="predicate"></param>
    /// <returns></returns>
    Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    /// Get or set a collection in cache
    /// </summary>
    /// <param name="key"></param>
    /// <param name="factory"></param>
    /// <param name="expiry"></param>
    /// <returns></returns>
    Task<TEntity?> GetOrSetAsync(string key, Func<Task<TEntity?>> factory, TimeSpan? expiry = null);

    /// <summary>
    /// Get or set a list of entities in cache
    /// </summary>
    /// <param name="key"></param>
    /// <param name="factory"></param>
    /// <param name="expiry"></param>
    /// <returns></returns>
    Task<List<TEntity>> GetOrSetListAsync(string key, Func<Task<List<TEntity>>> factory, TimeSpan? expiry = null);

    /// <summary>
    /// Remove an entity from cache by key
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    Task RemoveAsync(string key);
}