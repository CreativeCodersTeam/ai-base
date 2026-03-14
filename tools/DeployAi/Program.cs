using DeployAi.Languages;

namespace DeployAi;

internal static class Program
{
    internal static async Task<int> Main(string[] args)
    {
        var arguments = ReadArguments(args);

        var deployment = new AiConfigDeployment(new AiSystems.AiSystems(), new LanguageTypes());

        await deployment.DeployAsync(arguments);

        return 0;
    }

    private static ToolArguments ReadArguments(string[] args)
    {
        var config = new ToolArguments();

        foreach (var arg in args)
        {
            if (arg.StartsWith("--ai="))
            {
                config.AiSystems = arg.Substring("--ai=".Length).Split(",");
            }
            else if (arg.StartsWith("--languages="))
            {
                config.Languages = arg.Substring("--languages=".Length).Split(",");
            }
            else if (arg.StartsWith("--output="))
            {
                config.OutputDir = arg.Substring("--output=".Length);
            }
            else if (arg.StartsWith("--cleanup"))
            {
                config.CleanupBeforeDeployment = true;
            }
            else if (arg.StartsWith("--agents-md"))
            {
                config.PreferAgentsMd = true;
            }
        }

        return config;
    }
}
