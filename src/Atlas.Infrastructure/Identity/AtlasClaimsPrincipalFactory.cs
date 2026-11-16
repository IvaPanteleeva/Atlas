namespace Atlas.Infrastructure.Identity;

using System.Security.Claims;
using Atlas.Application.Identity;
using Atlas.Infrastructure.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

// Identity's default claims (id, name, security stamp, roles) plus the ClientViewer's company.
// The claim comes from AtlasUser.CompanyId every time the cookie is issued or refreshed,
// so nothing is stored in AspNetUserClaims.
public sealed class AtlasClaimsPrincipalFactory(
    UserManager<AtlasUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<AtlasUser, IdentityRole<Guid>>(userManager, roleManager, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AtlasUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        if (user.CompanyId is { } companyId)
        {
            identity.AddClaim(new Claim(AtlasClaims.CompanyId, companyId.ToString()));
        }

        return identity;
    }
}
