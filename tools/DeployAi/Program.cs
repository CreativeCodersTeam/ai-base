using DeployAi.Languages;
using Spectre.Console;

namespace DeployAi;

internal static class Program
{
    internal static int Main(string[] args)
    {
        var arguments = ReadArguments(args);

        var deployment = new AiConfigDeployment(new AiSystems.AiSystems(), new LanguageTypes());

        deployment.Deploy(arguments);

        return 0;
    }

    private static ToolArguments ReadArguments(string[] args)
    {
        var config = new ToolArguments();

        foreach (var arg in args)
        {
            if (arg.StartsWith("--ai="))
            {
                config.AiSystems = arg["--ai=".Length..].Split(",");
            }
            else if (arg.StartsWith("--languages="))
            {
                config.Languages = arg["--languages=".Length..].Split(",");
            }
            else if (arg.StartsWith("--output="))
            {
                config.OutputDir = arg["--output=".Length..];
            }
            else if (arg.StartsWith("--cleanup"))
            {
                config.CleanupBeforeDeployment = true;
            }
            else if (arg.StartsWith("--agents-md"))
            {
                config.PreferAgentsMd = true;
            }
            else if (arg.StartsWith("--project-markdown="))
            {
                config.ProjectMarkdownFile = arg["--project-markdown=".Length..];
            }
        }

        if (config.Languages.Contains("copilot", StringComparer.OrdinalIgnoreCase) && config.PreferAgentsMd)
        {
            AnsiConsole.WriteLine(
                "Disable --agents-md because Copilot is also configured and will be have duplicate instructions.");

            config.PreferAgentsMd = false;
        }

        return config;
    }
}
