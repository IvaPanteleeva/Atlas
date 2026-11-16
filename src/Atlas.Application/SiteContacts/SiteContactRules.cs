namespace Atlas.Application.SiteContacts;

using Atlas.Application.People;

// Site contact rules in one place (CLAUDE.md "Контакти на обекта"): the validator checks them,
// SiteContactConfiguration (Infrastructure) uses the same lengths. Name, phone and note have the same lengths
// as a Person; the text is normalized with PersonRules.
public static class SiteContactRules
{
    public const int NameMaxLength = PersonRules.NameMaxLength;

    public const int RoleMaxLength = 50;

    public const int PhoneMaxLength = PersonRules.PhoneMaxLength;

    public const int NoteMaxLength = PersonRules.NoteMaxLength;

    public const string NotFoundMessage = "Няма такъв контакт.";
}
