using System.Text;

namespace DeployAi.AiSystems.Claude;

public static class ClaudeRulesFrontmatter
{
    private const string Delimiter = "---";

    public static string Convert(string content)
    {
        var lines = content.Split('\n');

        if (lines.Length == 0 || lines[0].TrimEnd('\r') != Delimiter)
        {
            return content;
        }

        var endIndex = -1;
        for (var i = 1; i < lines.Length; i++)
        {
            if (lines[i].TrimEnd('\r') == Delimiter)
            {
                endIndex = i;
                break;
            }
        }

        if (endIndex == -1)
        {
            return content;
        }

        var globs = new List<string>();
        for (var i = 1; i < endIndex; i++)
        {
            var line = lines[i];
            var colonIndex = line.IndexOf(':');
            if (colonIndex <= 0)
            {
                continue;
            }

            var key = line[..colonIndex].Trim();
            if (key != "applyTo")
            {
                continue;
            }

            var value = line[(colonIndex + 1)..].Trim();
            value = StripQuotes(value);

            foreach (var part in value.Split(','))
            {
                var glob = part.Trim();
                if (glob.Length > 0)
                {
                    globs.Add(glob);
                }
            }
        }

        var body = string.Join('\n', lines, endIndex + 1, lines.Length - endIndex - 1);

        if (globs.Count == 0)
        {
            return body;
        }

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
