namespace Atlas.Application.Identity;

// Claims Atlas adds to the login cookie on top of Identity's own (id, name, roles).
public static class AtlasClaims
{
    // Company of a ClientViewer. Computed from AtlasUser.CompanyId at sign-in, not stored in AspNetUserClaims.
    public const string CompanyId = "company_id";
}
