namespace Atlas.Infrastructure.Entities;

using Microsoft.AspNetCore.Identity;

public class AtlasUser : IdentityUser<Guid>
{
    // Set only for ClientViewer users: the company whose sites they may see.
    public Guid? CompanyId { get; set; }

    public Company? Company { get; set; }
}
