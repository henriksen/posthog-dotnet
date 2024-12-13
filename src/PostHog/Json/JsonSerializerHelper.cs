using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PostHog.Json;

public static class JsonSerializerHelper
{
    public static async Task<string> SerializeToCamelCaseJsonStringAsync<T>(T obj, bool writeIndented = false)
    {
        var stream = await SerializeToCamelCaseJsonStreamAsync(obj, writeIndented);
        stream.Position = 0;
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        return Encoding.UTF8.GetString(memoryStream.ToArray());
    }

    public static async Task<Stream> SerializeToCamelCaseJsonStreamAsync<T>(T obj, bool writeIndented = false)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = writeIndented
        };

        var stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(stream, obj, options);
        return stream;
    }

    public static async Task<T?> DeserializeFromCamelCaseJson<T>(string json)
    {
        using var jsonStream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        return await DeserializeFromCamelCaseJsonAsync<T>(jsonStream);
    }

    public static async Task<T?> DeserializeFromCamelCaseJsonAsync<T>(
        Stream jsonStream,
        CancellationToken cancellationToken = default)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new ReadOnlyCollectionJsonConverterFactory(),
                new ReadonlyDictionaryJsonConverterFactory()
            }
        };

        return await JsonSerializer.DeserializeAsync<T>(jsonStream, options, cancellationToken);
    }
}