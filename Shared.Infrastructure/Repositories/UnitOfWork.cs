using Marten;
using Shared.Application.Interfaces;
using Shared.Application.Interfaces.Repositories;
using Shared.Infrastructure.Contexts;

namespace Shared.Infrastructure.Repositories;

public class UnitOfWork(AppDbContext context, IDocumentSession session) : IUnitOfWork
{
 
    /// <summary>
    /// The database context for the unit of work.
    /// </summary>
    public void Dispose()
    {
        context?.Dispose();
        session?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Begin a new transaction and execute the provided action.
    /// </summary>
    /// <param name="action"></param>
    /// <param name="cancellationToken"></param>
    public async Task BeginTransactionAsync(Func<Task<bool>> action, CancellationToken cancellationToken = default)
    {
        // Begin transaction
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Execute action
            if (await action())
            {
                await transaction.CommitAsync(cancellationToken);
            }
            else
            {
                await transaction.RollbackAsync(cancellationToken);
            }
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
    
    /// <summary>
    /// Save all changes to the database
    /// </summary>
    /// <param name="userName"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="needLogicalDelete"></param>
    /// <returns></returns>
    public async Task<int> SaveChangesAsync(string userName, CancellationToken cancellationToken = default, bool needLogicalDelete = false)
    {
        return await context.SaveChangesAsync(userName, cancellationToken, needLogicalDelete);
    }

    /// <summary>
    /// Store a collection of entities in the Marten session.
    /// </summary>
    /// <param name="entity"></param>
    /// <typeparam name="TCollection"></typeparam>
    public void Store<TCollection>(TCollection entity) where TCollection : class
    {
        session.Store(entity);
    }

    /// <summary>
    /// Delete a collection of entities from the Marten session.
    /// </summary>
    /// <param name="entity"></param>
    /// <typeparam name="TCollection"></typeparam>
    public void Delete<TCollection>(TCollection entity) where TCollection : class
    {
        session.Delete(entity);
    }

    /// <summary>
    /// Save all changes asynchronously
    /// </summary>
    public async Task SessionSaveChangesAsync()
    {
        await session.SaveChangesAsync();
    }
}