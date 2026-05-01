using System.Text;

namespace DeployAi.AiSystems.Claude;

public static class ClaudeRulesFrontmatter
{
    private const string Delimiter = "---";

    public static string Convert(string content)
    {
        var lines = content.Split('\n');

        var (startIndex, endIndex) = FindFrontmatterEnd(lines);
        if (startIndex == -1 || endIndex == -1)
        {
            return content;
        }

        var globs = ExtractApplyToGlobs(lines, startIndex, endIndex);
        var body = ExtractBody(lines, endIndex);

        return globs.Count == 0 ? body : BuildPathsFrontmatter(globs, body);
    }

    private static (int Start, int End) FindFrontmatterEnd(string[] lines)
    {
        var start = FindDelimiter(lines, 0);
        if (start == -1)
        {
            return (-1, -1);
        }

        var end = FindDelimiter(lines, start + 1);
        return (start, end);
    }

    private static int FindDelimiter(string[] lines, int from)
    {
        for (var i = from; i < lines.Length; i++)
        {
            if (lines[i].TrimEnd('\r') == Delimiter)
            {
                return i;
            }
        }

        return -1;
    }

    private static List<string> ExtractApplyToGlobs(string[] lines, int startIndex, int endIndex)
    {
        var globs = new List<string>();
        for (var i = startIndex + 1; i < endIndex; i++)
        {
            globs.AddRange(ParseApplyToLine(lines[i]));
        }

        return globs;
    }

    private static IEnumerable<string> ParseApplyToLine(string line)
    {
        var colonIndex = line.IndexOf(':');
        if (colonIndex <= 0)
        {
            yield break;
        }

        if (line[..colonIndex].Trim() != "applyTo")
        {
            yield break;
        }

        var value = StripQuotes(line[(colonIndex + 1)..].Trim());

        foreach (var part in value.Split(','))
        {
            var glob = part.Trim();
            if (glob.Length > 0)
            {
                yield return glob;
            }
        }
    }

    private static string ExtractBody(string[] lines, int endIndex)
        => string.Join('\n', lines, endIndex + 1, lines.Length - endIndex - 1);

    private static string BuildPathsFrontmatter(List<string> globs, string body)
    {
        var builder = new StringBuilder();
        builder.Append(Delimiter).Append('\n');
        builder.Append("paths:").Append('\n');
        foreach (var glob in globs)
        {
            builder.Append("  - \"").Append(glob).Append("\"\n");
        }
        builder.Append(Delimiter).Append('\n');
        builder.Append(body);

        return builder.ToString();
    }

    private static string StripQuotes(string value)
    {
        if (value.Length >= 2)
        {
            var first = value[0];
            var last = value[^1];
            if ((first == '\'' && last == '\'') || (first == '"' && last == '"'))
            {
                return value[1..^1];
            }
        }

        return value;
    }
}
