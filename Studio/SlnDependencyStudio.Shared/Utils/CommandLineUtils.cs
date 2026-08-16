using System.Text;

namespace SlnDependencyStudio.Shared.Utils;

/// <summary>Utility methods for splitting a command-line argument string into individual arguments.</summary>
public static class CommandLineUtils
{
    /// <summary>
    /// Splits a command-line argument string into individual arguments, honouring double-quoted
    /// segments so a quoted value containing spaces is preserved as a single argument.
    /// </summary>
    /// <param name="arguments">The argument string to split.</param>
    /// <returns>The individual arguments with quotes removed. An empty or whitespace-only string yields no arguments.</returns>
    /// <remarks>
    /// Examples:
    /// <code>
    /// SplitArguments("--file \"my file.txt\"")       -> ["--file", "my file.txt"]
    /// SplitArguments("--config Release")             -> ["--config", "Release"]
    /// SplitArguments("--message \"say \\\"hi\\\"\"") -> ["--message", "say \"hi\""]
    /// </code>
    /// A backslash escapes a following quote, producing a literal quote character. An unterminated
    /// quote consumes the remainder of the string as a single argument. Backslashes are otherwise
    /// preserved literally.
    /// </remarks>
    public static IReadOnlyList<string> SplitArguments(string arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        var result = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var index = 0; index < arguments.Length; index++)
        {
            var character = arguments[index];

            // A backslash escapes a following quote, producing a literal quote character.
            if (character == '\\' && index + 1 < arguments.Length && arguments[index + 1] == '"')
            {
                current.Append('"');
                index++;
                continue;
            }

            // A quote toggles quote mode; inside quotes whitespace is preserved.
            if (character == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            // Whitespace separates arguments, unless we are inside a quoted segment.
            if (char.IsWhiteSpace(character) && !inQuotes)
            {
                if (current.Length > 0)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }

                continue;
            }

            current.Append(character);
        }

        // An unterminated quote is tolerated: the trailing segment is emitted as a single argument.
        if (current.Length > 0)
        {
            result.Add(current.ToString());
        }

        return result;
    }
}
