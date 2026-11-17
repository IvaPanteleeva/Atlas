using Atlas.Application.Tasks;

namespace Atlas.UnitTests.Tasks;

public class TaskRulesTests
{
    [Fact]
    public void NormalizeText_Trims()
    {
        Assert.Equal("Купи лепило", TaskRules.NormalizeText("  Купи лепило "));
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [InlineData(true, false)]
    public void AssigneeError_CrewOrCurrent_IsAllowed(bool isCurrent, bool isCrew)
    {
        Assert.Null(TaskRules.AssigneeError(isCurrent, isCrew));
    }

    [Fact]
    public void AssigneeError_NewChoiceNotCrew_IsAnError()
    {
        Assert.Equal(TaskRules.AssigneeNotCrewMessage, TaskRules.AssigneeError(isCurrent: false, isCrew: false));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AssigneeError_UnknownPerson_IsAnError(bool isCurrent)
    {
        Assert.Equal(TaskRules.AssigneeNotFoundMessage, TaskRules.AssigneeError(isCurrent, isCrew: null));
    }
}
