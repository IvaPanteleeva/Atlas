namespace Atlas.Infrastructure.Entities;

// The company that gives us work and pays for it (e.g. ЖЕНИСТРОЙ ЕООД).
public class Company
{
    public Guid Id { get; set; }

    public required string Name { get; set; }
}
