using System.Linq.Expressions;

namespace Finova.Shared.Infrastructure.Interfaces;

public interface IBaseRepository<TEntity>
    where TEntity : class
{
    Task<TEntity> CreateAsync(TEntity entity, CancellationToken ct = default);

    Task DeleteAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
    Task<List<TEntity>> RetrieveAllAsync(CancellationToken ct = default);
    Task<IEnumerable<TEntity>> RetrieveAllAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default
    );
    Task<TEntity?> RetrieveAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default
    );

    Task<TEntity?> RetrieveWithIncludeAsync(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, object>> includePredicate,
        CancellationToken ct = default
    );

    TEntity Update(TEntity entity);

    Task<TEntity?> RetrieveTrackedAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default
    );

    Task<List<TEntity>> RetrieveAllTrackedAsync(CancellationToken ct = default);
}
