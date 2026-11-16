namespace Atlas.UnitTests.Fakes;

// A clock that always shows the same moment (hand-written fake, no mocking library).
public sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}
