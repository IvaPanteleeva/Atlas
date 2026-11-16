namespace Atlas.Infrastructure.Persistence.Repositories;

using Atlas.Application.Contracts;

public abstract class DataRepository<TEntity>(AtlasDbContext data) : IRepository<TEntity>
    where TEntity : class
{
    protected AtlasDbContext Data { get; } = data;

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken)
        => await this.Data.Set<TEntity>().AddAsync(entity, cancellationToken);

    public void Delete(TEntity entity)
        => this.Data.Set<TEntity>().Remove(entity);

    public async Task SaveAsync(CancellationToken cancellationToken)
        => await this.Data.SaveChangesAsync(cancellationToken);
}
