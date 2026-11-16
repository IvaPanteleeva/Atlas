namespace Atlas.Application.Identity;

// Allowed user name characters, in one place: Identity enforces them (IdentityOptionsSetup)
// and CreateUserRequestValidator checks them first, so the user sees a Bulgarian message.
public static class UserNameRules
{
    // Latin and Cyrillic letters (А-Я, а-я), digits and - . _ @. No spaces.
    public static readonly string AllowedCharacters =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"
        + CharRange('А', 'Я')
        + CharRange('а', 'я')
        + "0123456789-._@";

    public const string InvalidCharactersMessage =
        "Потребителското име може да съдържа само букви (латиница или кирилица), цифри и - . _ @, без интервали.";

    // Only the characters; an empty name is the NotEmpty rule's job.
    public static bool IsValid(string? userName)
        => userName is null || userName.All(AllowedCharacters.Contains);

    private static string CharRange(char first, char last)
        => new([.. Enumerable.Range(first, last - first + 1).Select(c => (char)c)]);
}
