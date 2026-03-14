using Spectre.Console;

namespace DeployAi.AiSystems.Claude;

public class ClaudeAiSystem() : AiSystemBase("claude", "Claude")
{
    public override async Task DeployAsync(DeploymentSetup setup)
    {
        AnsiConsole.WriteLine(
            $"Deploying {DisplayName} with the following languages: {string.Join(", ", setup.LanguageTypes.Select(l => l.DisplayName))}");
    }

    public override async Task CleanupAsync(DeploymentSetup setup)
    {
        AnsiConsole.WriteLine($"Cleaning up {DisplayName}...");
    }
}
