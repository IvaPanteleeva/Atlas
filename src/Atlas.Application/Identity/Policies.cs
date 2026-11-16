namespace Atlas.Application.Identity;

// Authorization policy names (CLAUDE.md "Вход и роли"). Registered in Atlas.Api AuthConfiguration.
public static class Policies
{
    // Owner only.
    public const string Owner = "Owner";

    // Our own people: Owner or Crew.
    public const string Staff = "Staff";

    // The client company (ClientViewer), read-only portal.
    public const string Client = "Client";
}
