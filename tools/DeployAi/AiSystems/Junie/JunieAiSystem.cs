using Spectre.Console;

namespace DeployAi.AiSystems.Junie;

public class JunieAiSystem() : AiSystemBase("junie", "Junie")
{
    public override void Deploy(DeploymentSetup setup)
    {
        AnsiConsole.WriteLine(
            $"Deploying {DisplayName} with the following languages: {string.Join(", ", setup.LanguageTypes.Select(l => l.DisplayName))}");

        var paths = new JuniePaths(setup.OutputDir);

        var instructionsFile = setup.PreferAgentsMd ? paths.AgentsFile : paths.GuidelinesFile;

        WriteFile(instructionsFile, CombineInstructionFiles(setup));
    }

    public override void Cleanup(DeploymentSetup setup)
    {
        AnsiConsole.WriteLine($"Cleaning up {DisplayName}...");

        var paths = new JuniePaths(setup.OutputDir);

        CleanupFile(paths.GuidelinesFile);
        CleanupFile(paths.AgentsFile);
    }
}
