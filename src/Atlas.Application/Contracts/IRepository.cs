namespace Atlas.Application.Contracts;

// Base for every repository. Smaller than Gemsoft's: async only, Delete just marks the entity,
// SaveAsync commits everything tracked in the request.
public interface IRepository<in TEntity>
    where TEntity : class
{
    Task AddAsync(TEntity entity, CancellationToken cancellationToken);

    void Delete(TEntity entity);

    Task SaveAsync(CancellationToken cancellationToken);
}
