using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Finova.Shared.Infrastructure.Interfaces;

public interface IBaseRepository<TEntity>
    where TEntity : class
{
    Task<TEntity> CreateAsync(TEntity entity);
    Task DeleteAsync(Expression<Func<TEntity, bool>> predicate);
    Task<List<TEntity>> RetrieveAllAsync();
    Task<IEnumerable<TEntity>> RetrieveAllAsync(Expression<Func<TEntity, bool>> predicate);
    Task<TEntity?> RetrieveAsync(Expression<Func<TEntity, bool>> predicate);

    Task<TEntity?> RetrieveWithIncludeAsync(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, object>> includePredicate
    );
    TEntity Update(TEntity entity);

    Task<TEntity?> RetrieveTrackedAsync(Expression<Func<TEntity, bool>> predicate);

    Task<List<TEntity>> RetrieveAllTrackedAsync();
}
