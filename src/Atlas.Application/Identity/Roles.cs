namespace Atlas.Application.Identity;

// CLAUDE.md "Вход и роли" (round 28): Owner „Администратор“ (everything), Manager „Управител“ (everything except
// deleting and login profiles), ClientViewer „Възложител“ (read-only portal). The former Crew role became Manager
// (migration RenameCrewToManager). Workers have no login: they are only people in „Хора“.
public static class Roles
{
    public const string Owner = "Owner";

    public const string Manager = "Manager";

    public const string ClientViewer = "ClientViewer";

    public static readonly IReadOnlyList<string> All = [Owner, Manager, ClientViewer];

    // A person linked to a profile with one of these roles is a partner („съдружник“, CLAUDE.md "Разход за хора").
    public static readonly IReadOnlyList<string> Partner = [Owner, Manager];
}
