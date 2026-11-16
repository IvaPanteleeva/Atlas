namespace Atlas.Application.Identity;

public static class Roles
{
    public const string Owner = "Owner";

    public const string Crew = "Crew";

    public const string ClientViewer = "ClientViewer";

    public static readonly IReadOnlyList<string> All = [Owner, Crew, ClientViewer];
}
