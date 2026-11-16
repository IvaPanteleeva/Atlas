namespace Atlas.Api.Endpoints;

using Atlas.Api.Common;
using Atlas.Application.Identity;
using Atlas.Application.Payments;
using Atlas.Infrastructure.Persistence.Repositories;

// Client payments of one site (CLAUDE.md "Плащания и суми"): Owner and Manager see, add and edit them;
// only the Owner deletes.
public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sites/{siteId:guid}/payments")
            .WithTags("Payments")
            .RequireAuthorization(Policies.Staff);

        group.MapGet("/", GetPayments)
            .WithName("GetPayments")
            .WithSummary("List the payments of a site")
            .WithDescription("The client payments of the site, newest first (date, then entry time).")
            .Produces<IReadOnlyList<PaymentResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CreatePayment)
            .WithValidation<SavePaymentRequest>()
            .WithName("CreatePayment")
            .WithSummary("Add a payment")
            .WithDescription("Adds a client payment: amount over 0 up to 1 000 000 € with 2 decimals, date today or earlier.")
            .Produces<PaymentResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", UpdatePayment)
            .WithValidation<SavePaymentRequest>()
            .WithName("UpdatePayment")
            .WithSummary("Edit a payment")
            .WithDescription("Replaces the date, amount and note of the payment.")
            .Produces<PaymentResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", DeletePayment)
            .RequireAuthorization(Policies.Owner)
            .WithName("DeletePayment")
            .WithSummary("Delete a payment")
            .WithDescription("Deletes the payment.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> GetPayments(
        Guid siteId,
        IPaymentRepository payments,
        CancellationToken cancellationToken)
        => (await payments.ListAsync(siteId, cancellationToken)).ToHttpResult();

    private static async Task<IResult> CreatePayment(
        Guid siteId,
        SavePaymentRequest request,
        IPaymentRepository payments,
        CancellationToken cancellationToken)
        => (await payments.CreateAsync(siteId, request, cancellationToken))
            .ToHttpResult(payment => TypedResults.Created($"/api/sites/{siteId}/payments/{payment.Id}", payment));

    private static async Task<IResult> UpdatePayment(
        Guid siteId,
        Guid id,
        SavePaymentRequest request,
        IPaymentRepository payments,
        CancellationToken cancellationToken)
        => (await payments.UpdateAsync(siteId, id, request, cancellationToken)).ToHttpResult();

    private static async Task<IResult> DeletePayment(
        Guid siteId,
        Guid id,
        IPaymentRepository payments,
        CancellationToken cancellationToken)
        => (await payments.DeleteAsync(siteId, id, cancellationToken)).ToHttpResult();
}
