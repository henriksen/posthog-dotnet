using System.Text.Json;
using System.Text.Json.Serialization;

namespace PostHog.Json;

public class PropertyFilterValueJsonConverter : JsonConverter<PropertyFilterValue>
{
    public override PropertyFilterValue? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var filterElement = JsonDocument.ParseValue(ref reader).RootElement;
        return PropertyFilterValue.Create(filterElement);
    }

    public override void Write(Utf8JsonWriter writer, PropertyFilterValue value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}