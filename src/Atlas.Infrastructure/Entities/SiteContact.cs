namespace Atlas.Infrastructure.Entities;

// A contact of one site: the owner of the flat, a neighbour, ... (CLAUDE.md "Контакти на обекта").
// Separate from Person (our crew and global contacts); cannot be put on a work day.
public class SiteContact
{
    public Guid Id { get; set; }

    public Guid SiteId { get; set; }

    public Site? Site { get; set; }

    public required string Name { get; set; }

    // Free text: „собственик“, „съсед“, ...
    public string? Role { get; set; }

    public string? Phone { get; set; }

    public string? Note { get; set; }
}
