namespace Atlas.Application.Identity;

// Authorization policy names (CLAUDE.md "Вход и роли"). Registered in Atlas.Api AuthConfiguration.
public static class Policies
{
    // Owner only: deleting and login profiles (/api/users).
    public const string Owner = "Owner";

    // Our own people: Owner or Manager. Everything except deleting and login profiles.
    public const string Staff = "Staff";

    // The client company (ClientViewer), read-only portal.
    public const string Client = "Client";
}
