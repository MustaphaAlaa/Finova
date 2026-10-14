using System.Linq.Expressions;
using Finova.Shared.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore; 

namespace Shared.Infrastructure.Repositories;

public class BaseRepository<TEntity, TDbContext> : IBaseRepository<TEntity>
    where TEntity : class
    where TDbContext : DbContext
{
    protected readonly TDbContext _db;

    public BaseRepository(TDbContext context)
    {
        _db = context;
    }

    /// <summary>
    /// Create an entity in the database.
    /// </summary>
    /// <param name="entity">the entity type to be creat.</param>
    /// <returns>the entity after created it.</returns>
    public async Task<TEntity> CreateAsync(TEntity entity)
    {
        await _db.Set<TEntity>().AddAsync(entity);
        return entity;
    }

    /// <summary>
    /// Asynchronously deletes an entity from the database based on a predicate.
    /// </summary>
    /// <param name="predicate">The condition to find the entity to be deleted.</param>
    /// <returns>The number of state entries written to the database.</returns>
    public async Task DeleteAsync(Expression<Func<TEntity, bool>> predicate)
    {
        var entity = await _db.Set<TEntity>().FirstOrDefaultAsync(predicate);

        if (entity == null)
            return;

        _db.Set<TEntity>().Remove(entity);
    }

    public async Task<TEntity?> RetrieveAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return await _db.Set<TEntity>().AsNoTracking().FirstOrDefaultAsync(predicate);
    }

    public async Task<TEntity?> RetrieveWithIncludeAsync(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, object>> includePredicate
    )
    {
        return await _db.Set<TEntity>()
            .AsNoTracking()
            .Include(includePredicate)
            .FirstOrDefaultAsync(predicate);
    }

    public Task<List<TEntity>> RetrieveAllAsync()
    {
        return _db.Set<TEntity>().AsNoTracking().Select(entity => entity).ToListAsync();
    }

    public async Task<IEnumerable<TEntity>> RetrieveAllAsync(
        Expression<Func<TEntity, bool>> predicate
    ) // change the return type
    {
        return await _db.Set<TEntity>().AsNoTracking().Where(predicate).ToListAsync();
    }

    public TEntity Update(TEntity entity)
    {
        _db.Set<TEntity>().Update(entity);
        return entity;
    }

    // Example of a retrieval method FOR UPDATE scenarios
    public async Task<TEntity?> RetrieveTrackedAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return await _db.Set<TEntity>().FirstOrDefaultAsync(predicate);
    }

    public async Task<List<TEntity>> RetrieveAllTrackedAsync()
    {
        return await _db.Set<TEntity>().ToListAsync();
    }
 
}
