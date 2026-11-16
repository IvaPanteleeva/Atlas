namespace Atlas.Api.Common;

using System.Security.Claims;
using Atlas.Application.Contracts;
using Atlas.Application.Identity;

// Reads the signed-in user from the request's claims (set by AtlasClaimsPrincipalFactory at sign-in).
public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => this.Principal?.Identity?.IsAuthenticated == true;

    public Guid? UserId => ParseGuid(this.Claim(ClaimTypes.NameIdentifier));

    public string? UserName => this.Claim(ClaimTypes.Name);

    public string? Role => this.Claim(ClaimTypes.Role);

    public Guid? CompanyId => ParseGuid(this.Claim(AtlasClaims.CompanyId));

    private string? Claim(string type)
        => this.IsAuthenticated ? this.Principal!.FindFirstValue(type) : null;

    private static Guid? ParseGuid(string? value)
        => Guid.TryParse(value, out var id) ? id : null;
}
