using Spectre.Console;

namespace DeployAi.AiSystems.Junie;

public class JunieAiSystem() : AiSystemBase("junie", "Junie")
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
