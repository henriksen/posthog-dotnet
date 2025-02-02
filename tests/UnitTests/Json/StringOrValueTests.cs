using PostHog.Json;

namespace StringOrValueTests;

public class AssignmentTests
{
    [Fact]
    public void CanImplicitlyAssignString()
    {
        StringOrValue<bool> result = "bilbo";

        Assert.Equal("bilbo", result);
        Assert.True(result.IsString);
        Assert.False(result.IsValue);
    }

    [Fact]
    public void CanImplicitlyAssignBool()
    {
        StringOrValue<bool> result = true;

        Assert.True(result.Value);
        Assert.False(result.IsString);
        Assert.True(result.IsValue);
    }
}

public class EqualityTests
{
    [Fact]
    public void CanCompareStringOrValue()
    {
        var stringOrBool1 = new StringOrValue<bool>("scooby");
        var stringOrBool2 = new StringOrValue<bool>("scooby");
        var stringOrInt = new StringOrValue<int>(42);

        Assert.Equal(stringOrBool1, stringOrBool2);
        Assert.Equal("scooby", stringOrBool2);
        Assert.Equal(42, stringOrInt);
        Assert.NotEqual(43, stringOrInt);
    }

    [Fact]
    public void CanCompareDefaults()
    {
        StringOrValue<bool> undefined = default;
        StringOrValue<bool> anotherUndefined = default;
        StringOrValue<bool> defined = new(true);

        Assert.Equal(default, undefined);
        Assert.Equal(anotherUndefined, undefined);
        Assert.NotEqual(defined, undefined);
        Assert.NotEqual(defined, default);
    }

    [Fact]
    public void CanCompareNullables()
    {
        StringOrValue<bool>? value = new(true);
        StringOrValue<bool>? anotherValue = new(true);
        StringOrValue<bool>? undefined = null;

        Assert.Equal(anotherValue, value);
        Assert.Null(undefined);
        Assert.NotEqual(anotherValue, undefined);
    }
}