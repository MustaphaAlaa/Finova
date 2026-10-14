using System.Linq.Expressions;

namespace BankSimulator.Application;

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
