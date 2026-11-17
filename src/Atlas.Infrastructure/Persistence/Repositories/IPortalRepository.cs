namespace Atlas.Infrastructure.Persistence.Repositories;

using Atlas.Application.Common;
using Atlas.Application.Contracts;
using Atlas.Application.Portal;
using Atlas.Infrastructure.Entities;

// Read-only views for the client company (CLAUDE.md "Достъп за възложителя"). Every query is filtered by the
// company id, which the caller takes from the signed-in user (ICurrentUser.CompanyId), never from the request.
// Projects straight into the portal DTOs: notes, people, worker sums, contacts, tasks and site prices are never read.
public interface IPortalRepository : IRepository<Site>
{
    // The company's sites at every status, with their sums and „Дължите“.
    Task<PortalSitesResponse> ListAsync(Guid companyId, CancellationToken cancellationToken);

    // One site of the company. NotFound for a site of another company, a site without a company or an unknown id.
    Task<Result<PortalSiteResponse>> GetAsync(Guid companyId, Guid siteId, CancellationToken cancellationToken);
}
