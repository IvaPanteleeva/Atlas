namespace Atlas.Application.Sites;

/// <summary>Where a site is in its life (CLAUDE.md "Обекти и фирми"). Serialized as a string.</summary>
public enum SiteStatus
{
    /// <summary>„Оглед / оферта“: not started yet. A new site starts here.</summary>
    Lead,

    /// <summary>„Текущ“: work is going on.</summary>
    Active,

    /// <summary>„Приключен“: finished.</summary>
    Done,
}
