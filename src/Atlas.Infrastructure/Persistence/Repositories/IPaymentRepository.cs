namespace Atlas.Infrastructure.Persistence.Repositories;

using Atlas.Application.Common;
using Atlas.Application.Contracts;
using Atlas.Application.Payments;
using Atlas.Infrastructure.Entities;

// Lives next to the entity (not in Application): IRepository<ClientPayment> needs the ClientPayment type.
public interface IPaymentRepository : IRepository<ClientPayment>
{
    // The payments of one site, newest first (date, then entry time). NotFound when there is no such site.
    Task<Result<IReadOnlyList<PaymentResponse>>> ListAsync(Guid siteId, CancellationToken cancellationToken);

    // NotFound when there is no such site. The note is stored trimmed, empty → null.
    Task<Result<PaymentResponse>> CreateAsync(
        Guid siteId,
        SavePaymentRequest request,
        CancellationToken cancellationToken);

    // NotFound when the payment does not exist or belongs to another site.
    Task<Result<PaymentResponse>> UpdateAsync(
        Guid siteId,
        Guid id,
        SavePaymentRequest request,
        CancellationToken cancellationToken);

    // NotFound when the payment does not exist or belongs to another site.
    Task<Result> DeleteAsync(Guid siteId, Guid id, CancellationToken cancellationToken);
}
