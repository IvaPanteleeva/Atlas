namespace Atlas.Application.Tasks;

// Task rules in one place (CLAUDE.md "Задачи"): the validators check them, SiteTaskConfiguration (Infrastructure)
// uses the same length, SiteTaskRepository checks the person responsible with AssigneeError.
public static class TaskRules
{
    public const int TextMaxLength = 500;

    // GET /api/tasks?scope=company: only the tasks without a site.
    public const string CompanyScope = "company";

    public const string NotFoundMessage = "Няма такава задача.";

    public const string AssigneeNotFoundMessage = "Няма такъв човек.";

    public const string AssigneeNotCrewMessage = "Отговорникът трябва да е от екипа.";

    public const string UnknownScopeMessage = "Непознат обхват.";

    // The text is stored without leading and trailing spaces.
    public static string NormalizeText(string text)
        => text.Trim();

    // Who may be responsible (CLAUDE.md "Задачи"): isCrew is null for an unknown person. A new choice must be crew;
    // the current person responsible stays even when no longer crew.
    public static string? AssigneeError(bool isCurrent, bool? isCrew)
        => isCrew switch
        {
            null => AssigneeNotFoundMessage,
            false when !isCurrent => AssigneeNotCrewMessage,
            _ => null,
        };
}
