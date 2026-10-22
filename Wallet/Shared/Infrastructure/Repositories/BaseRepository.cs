using System.Linq.Expressions;
using Finova.Shared.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Shared.Infrastructure.Repositories;

public class BaseRepository<TEntity, TDbContext>(TDbContext context) : IBaseRepository<TEntity>
    where TEntity : class
    where TDbContext : DbContext
{
    protected readonly TDbContext Db = context;

    /// <summary>
    /// Create an entity in the database.
    /// </summary>
    /// <param name="entity">the entity type to be creat.</param>
    /// <param name="ct"> CancellationToken cancellationToken</param>
    /// <returns>the entity after created it.</returns>
    public async Task<TEntity> CreateAsync(TEntity entity, CancellationToken ct = default)
    {
        await Db.Set<TEntity>().AddAsync(entity, ct);
        return entity;
    }

    /// <summary>
    /// Asynchronously deletes an entity from the database based on a predicate.
    /// </summary>
    /// <param name="predicate">The condition to find the entity to be deleted.</param>
    /// <param name="ct"> CancellationToken cancellationToken</param>
    /// <returns>The number of state entries written to the database.</returns>
    public async Task DeleteAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default
    )
    {
        var entity = await Db.Set<TEntity>().FirstOrDefaultAsync(predicate, ct);

        if (entity == null)
            return;

        Db.Set<TEntity>().Remove(entity);
    }

    public async Task<TEntity?> RetrieveAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default
    )
    {
        return await Db.Set<TEntity>().AsNoTracking().FirstOrDefaultAsync(predicate, ct);
    }

    public async Task<TEntity?> RetrieveWithIncludeAsync(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, object>> includePredicate,
        CancellationToken ct = default
    )
    {
        return await Db.Set<TEntity>()
            .AsNoTracking()
            .Include(includePredicate)
            .FirstOrDefaultAsync(predicate, ct);
    }

    public async Task<List<TEntity>> RetrieveAllAsync(CancellationToken ct = default)
    {
        var result = await Db.Set<TEntity>()
            .AsNoTracking()
            .Select(entity => entity)
            .ToListAsync(ct);
        return result;
    }

    public async Task<IEnumerable<TEntity>> RetrieveAllAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default
    )
    {
        return await Db.Set<TEntity>().AsNoTracking().Where(predicate).ToListAsync(ct);
    }

    public TEntity Update(TEntity entity)
    {
        Db.Set<TEntity>().Update(entity);
        return entity;
    }

    // Example of a retrieval method FOR UPDATE scenarios
    public async Task<TEntity?> RetrieveTrackedAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default
    )
    {
        return await Db.Set<TEntity>().FirstOrDefaultAsync(predicate, ct);
    }

    public async Task<List<TEntity>> RetrieveAllTrackedAsync(CancellationToken ct = default)
    {
        return await Db.Set<TEntity>().ToListAsync(ct);
    }
}
