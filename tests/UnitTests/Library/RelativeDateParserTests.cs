using System.Globalization;
using PostHog.Library;

namespace RelativeDateParserTests;

public class TheParseMethod
{
    [Theory]
    [InlineData("-000h", "2024-01-22T22:15:50Z", "2024-01-21T16:15:49Z")]
    [InlineData("-30h", "2024-01-22T22:15:50Z", "2024-01-21T16:15:49Z")]
    [InlineData("-24d", "2024-01-22T22:15:50Z", "2023-12-29T22:15:49Z")]
    [InlineData("-2w", "2024-01-22T22:15:50Z", "2024-01-08T22:15:49Z")]
    [InlineData("-1m", "2024-01-22T22:15:50Z", "2023-12-22T22:15:49Z")]
    [InlineData("-1y", "2024-01-22T22:15:50Z", "2023-01-22T22:15:49Z")]
    public void CanCompareSpecifiedDateWithRelativeDate(string relativeDateString, string nowDate, string expectedBefore)
    {
        var now = DateTimeOffset.Parse(nowDate, CultureInfo.InvariantCulture);
        var beforeDate = DateTimeOffset.Parse(expectedBefore, CultureInfo.InvariantCulture);

        var relativeDate = RelativeDate.Parse(relativeDateString);

        Assert.NotNull(relativeDate);
        Assert.True(relativeDate.IsDateBefore(beforeDate, now));
    }

    [Theory]
    [InlineData("1")]
    [InlineData("1x")]
    [InlineData("1.2y")]
    [InlineData("1z")]
    [InlineData("1s")]
    [InlineData("-u10_001h")]
    [InlineData("10_001h")]
    [InlineData("bazinga")]
    [InlineData("")]
    public void ReturnsNullForBadFormats(string relativeDateString)
    {
        Assert.Null(RelativeDate.Parse(relativeDateString));
    }
}