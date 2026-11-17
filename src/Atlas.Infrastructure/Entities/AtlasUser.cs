namespace Atlas.Infrastructure.Entities;

using Microsoft.AspNetCore.Identity;

public class AtlasUser : IdentityUser<Guid>
{
    // Set only for ClientViewer users: the company whose sites they may see.
    public Guid? CompanyId { get; set; }

    public Company? Company { get; set; }

    // The crew member this profile belongs to (Owner or Manager only). Such a person is a partner („съдружник“):
    // on the work days without a pay (CLAUDE.md "Разход за хора"). Null for most profiles.
    public Guid? PersonId { get; set; }

    public Person? Person { get; set; }
}
