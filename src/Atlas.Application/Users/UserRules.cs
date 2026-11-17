namespace Atlas.Application.Users;

// Messages about login profiles linked to people (CLAUDE.md "Разход за хора": a partner is a crew member linked to an
// Owner or Manager profile).
public static class UserRules
{
    public const string ClientViewerPersonMessage = "Възложителят не може да е човек от екипа.";

    public const string PersonTakenMessage = "Човекът вече е свързан с друг профил.";
}
