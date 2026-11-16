namespace Atlas.Application.Contracts;

// The signed-in user of the current request, read from the cookie's claims.
// All values are null when nobody is signed in.
public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    Guid? UserId { get; }

    string? UserName { get; }

    string? Role { get; }

    // Only for ClientViewer users.
    Guid? CompanyId { get; }
}
