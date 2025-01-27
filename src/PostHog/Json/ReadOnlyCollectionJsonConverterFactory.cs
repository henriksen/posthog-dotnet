using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;

internal sealed class ReadOnlyCollectionJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsGenericType)
        {
            return false;
        }

        var genericTypeDefinition = typeToConvert.GetGenericTypeDefinition();
        return genericTypeDefinition == typeof(IReadOnlyCollection<>)
            || genericTypeDefinition == typeof(ReadOnlyCollection<>)
            || genericTypeDefinition == typeof(IReadOnlyList<>);
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var elementType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(ReadOnlyCollectionJsonConverterFactory<>).MakeGenericType(elementType);

        return (JsonConverter?)Activator.CreateInstance(converterType);
    }
}

internal sealed class ReadOnlyCollectionJsonConverterFactory<T> : JsonConverter<IEnumerable<T>>
{
    public override IEnumerable<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var list = JsonSerializer.Deserialize<List<T>>(ref reader, options);
        return list == null ? null : new ReadOnlyCollection<T>(list);
    }

    public override void Write(Utf8JsonWriter writer, IEnumerable<T> value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, value, options);
}