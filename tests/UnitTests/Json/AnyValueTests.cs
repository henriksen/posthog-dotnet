
using PostHog.Json;

public class AnyValueTests
{
    public class EqualityTests
    {
        [Fact]
        public void CanCompareWithOtherAny()
        {
            var stringValue = new AnyValue("scooby");
            var anotherStringValue = new AnyValue("scooby");
            var thirdStringValue = new AnyValue("shaggy");

            Assert.Equal(stringValue, stringValue);
            Assert.Equal(anotherStringValue, stringValue);
            Assert.NotEqual(thirdStringValue, stringValue);
        }
    }
}