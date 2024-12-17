using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PostHog.Json;

internal static class JsonSerializerHelper
{
    static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new ReadOnlyCollectionJsonConverterFactory(),
            new ReadonlyDictionaryJsonConverterFactory()
        }
    };
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
        var stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(stream, obj, Options);
        return stream;
    }

    public static async Task<T?> DeserializeFromCamelCaseJsonStringAsync<T>(string json)
    {
        using var jsonStream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        jsonStream.Position = 0;
        return await DeserializeFromCamelCaseJsonAsync<T>(jsonStream);
    }

    public static async Task<T?> DeserializeFromCamelCaseJsonAsync<T>(
        Stream jsonStream,
        CancellationToken cancellationToken = default) =>
        await JsonSerializer.DeserializeAsync<T>(jsonStream, Options, cancellationToken);
}