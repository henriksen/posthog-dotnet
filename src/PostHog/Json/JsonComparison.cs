namespace PostHog.Json;

using System.Text.Json;

public static class JsonComparison
{
    static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
    {
        WriteIndented = false // Minify the JSON
    };
    public static bool AreJsonElementsEqual(JsonElement element1, JsonElement element2)
    {
        // Serialize both JsonElements to their canonical form
        var normalized1 = JsonSerializer.Serialize(element1, SerializerOptions);

        var normalized2 = JsonSerializer.Serialize(element2, SerializerOptions);

        // Compare the normalized JSON strings
        return normalized1 == normalized2;
    }
}
