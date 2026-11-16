namespace Atlas.Infrastructure.Persistence.Repositories;

using System.Linq.Expressions;
using Atlas.Application.Common;
using Atlas.Application.Payments;
using Atlas.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

public sealed class PaymentRepository(AtlasDbContext data, TimeProvider timeProvider)
    : DataRepository<ClientPayment>(data), IPaymentRepository
{
    private static readonly Expression<Func<ClientPayment, PaymentResponse>> ToResponseExpression =
        p => new PaymentResponse(p.Id, p.SiteId, p.Date, p.Amount, p.Note);

    public async Task<Result<IReadOnlyList<PaymentResponse>>> ListAsync(
        Guid siteId,
        CancellationToken cancellationToken)
    {
        if (!await this.SiteExistsAsync(siteId, cancellationToken))
        {
            return Error.NotFound(SiteRepository.NotFoundMessage);
        }

        // Newest first: by date, then the one entered later (like the work days). Id only makes ties stable.
        var payments = await this.Data.ClientPayments
            .AsNoTracking()
            .Where(p => p.SiteId == siteId)
            .OrderByDescending(p => p.Date)
            .ThenByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Select(ToResponseExpression)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<PaymentResponse>>.SuccessWith(payments);
    }

    public async Task<Result<PaymentResponse>> CreateAsync(
        Guid siteId,
        SavePaymentRequest request,
        CancellationToken cancellationToken)
    {
        if (!await this.SiteExistsAsync(siteId, cancellationToken))
        {
            return Error.NotFound(SiteRepository.NotFoundMessage);
        }

        var payment = new ClientPayment { SiteId = siteId, CreatedAt = timeProvider.GetUtcNow() };
        Apply(payment, request);
        await this.AddAsync(payment, cancellationToken);

        try
        {
            await this.SaveAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.ForeignKeyViolation,
        })
        {
            // The site was deleted between the check and the save.
            return Error.NotFound(SiteRepository.NotFoundMessage);
        }

        return ToResponse(payment);
    }

    public async Task<Result<PaymentResponse>> UpdateAsync(
        Guid siteId,
        Guid id,
        SavePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var payment = await this.FindAsync(siteId, id, cancellationToken);
        if (payment is null)
        {
            return Error.NotFound(PaymentRules.NotFoundMessage);
        }

        Apply(payment, request);
        await this.SaveAsync(cancellationToken);

        return ToResponse(payment);
    }

    public async Task<Result> DeleteAsync(Guid siteId, Guid id, CancellationToken cancellationToken)
    {
        var payment = await this.FindAsync(siteId, id, cancellationToken);
        if (payment is null)
        {
            return Error.NotFound(PaymentRules.NotFoundMessage);
        }

        this.Delete(payment);
        await this.SaveAsync(cancellationToken);

        return Result.Success;
    }

    private static PaymentResponse ToResponse(ClientPayment payment)
        => new(payment.Id, payment.SiteId, payment.Date, payment.Amount, payment.Note);

    private static void Apply(ClientPayment payment, SavePaymentRequest request)
    {
        // The validator makes Date and Amount required; the fallbacks only keep the compiler happy.
        payment.Date = request.Date ?? payment.Date;
        payment.Amount = request.Amount ?? payment.Amount;
        payment.Note = PaymentRules.NormalizeNote(request.Note);
    }

    private async Task<bool> SiteExistsAsync(Guid siteId, CancellationToken cancellationToken)
        => await this.Data.Sites.AnyAsync(s => s.Id == siteId, cancellationToken);

    // A payment is only found through its own site: the id of another site's payment is a 404 here.
    private async Task<ClientPayment?> FindAsync(Guid siteId, Guid id, CancellationToken cancellationToken)
        => await this.Data.ClientPayments.FirstOrDefaultAsync(
            p => p.Id == id && p.SiteId == siteId,
            cancellationToken);
}
