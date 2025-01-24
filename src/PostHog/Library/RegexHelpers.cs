using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace PostHog.Library;

public static class RegexHelpers
{
    /// <summary>
    /// Validates that the pattern is a valid regular expression and if so, returns a <see cref="Regex"/> object.
    /// </summary>
    /// <param name="pattern">The regular expression pattern.</param>
    /// <param name="regex">The <see cref="Regex"/> object if the pattern is valid.</param>
    /// <param name="options">The <see cref="RegexOptions"/> to pass to the created <see cref="Regex"/>.</param>
    /// <returns><c>true</c> if the pattern is a valid regular expression. Otherwise <c>false</c></returns>
    public static bool TryValidateRegex(
        string pattern,
        [NotNullWhen(true)] out Regex? regex,
        RegexOptions options)
    {
        regex = null;
        if (string.IsNullOrEmpty(pattern))
        {
            return false;
        }

        try
        {
            regex = new Regex(pattern, options);
            return true;
        }
        catch (RegexParseException)
        {
            return false;
        }
    }
}