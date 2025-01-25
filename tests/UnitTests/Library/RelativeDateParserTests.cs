using System.Globalization;
using Microsoft.Extensions.Time.Testing;
using PostHog.Library;

namespace RelativeDateParserTests;

public class TheParseRelativeDateForFeatureFlagMethod
{
    [Theory]
    [InlineData("-30h", "2024-01-22T22:15:50Z", "2024-01-21T16:15:49Z", "2024-01-21T16:15:51Z")]
    [InlineData("-24d", "2024-01-22T22:15:50Z", "2023-12-29T22:15:49Z", "2023-12-29T22:15:51Z")]
    [InlineData("-2w", "2024-01-22T22:15:50Z", "2024-01-08T22:15:49Z", "2024-01-08T22:15:51Z")]
    [InlineData("-1m", "2024-01-22T22:15:50Z", "2023-12-22T22:15:49Z", "2023-12-22T22:15:51Z")]
    [InlineData("-1y", "2024-01-22T22:15:50Z", "2023-01-22T22:15:49Z", "2023-01-22T22:15:50Z")]
    public void CanCompareSpecifiedDateWithRelativeDate(string relativeDateString, string nowDate, string expectedBefore, string expectedAfter)
    {
        var now = DateTimeOffset.Parse(nowDate, CultureInfo.InvariantCulture);
        var beforeDate = DateTimeOffset.Parse(expectedBefore, CultureInfo.InvariantCulture);
        var afterDate = DateTimeOffset.Parse(expectedAfter, CultureInfo.InvariantCulture);

        var relativeDate = RelativeDate.Parse(relativeDateString);

        Assert.NotNull(relativeDate);
        Assert.True(relativeDate.IsDateBefore(beforeDate, now));
        Assert.True(relativeDate.IsDateAfter(afterDate, now));
    }
}