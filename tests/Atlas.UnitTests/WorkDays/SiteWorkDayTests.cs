using Atlas.Application.Sites;
using Atlas.Infrastructure.Entities;

namespace Atlas.UnitTests.WorkDays;

public class SiteWorkDayTests
{
    [Theory]
    [InlineData(SiteStatus.Lead, SiteStatus.Active)]
    [InlineData(SiteStatus.Active, SiteStatus.Active)]
    [InlineData(SiteStatus.Done, SiteStatus.Done)]
    public void OnWorkDayAdded_MakesALeadActive_AndLeavesTheOthers(SiteStatus before, SiteStatus after)
    {
        var site = new Site { Name = "Баня", Status = before };

        site.OnWorkDayAdded();

        Assert.Equal(after, site.Status);
    }
}
