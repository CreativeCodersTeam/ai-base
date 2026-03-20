using Spectre.Console;

namespace DeployAi.AiSystems.Copilot;

public class CopilotAiSystem() : AiSystemBase("copilot", "Copilot")
{
    public override void Deploy(DeploymentSetup setup)
    {
        AnsiConsole.WriteLine(
            $"Deploying {DisplayName} with the following languages: {string.Join(", ", setup.LanguageTypes.Select(l => l.DisplayName))}");

        var paths = new CopilotPaths(setup.OutputDir);

        CopyFile(GetGeneralInstructionFilePath(setup.SourceBaseDir), paths.CopilotFile);

        CopyAgentFiles(setup, paths.AgentsDir);

        CopyInstructionFiles(setup, paths.InstructionsDir);

        CopySkillFiles(setup, paths.SkillsDir);
    }

    public override void Cleanup(DeploymentSetup setup)
    {
        AnsiConsole.WriteLine($"Cleaning up {DisplayName}...");

        var paths = new CopilotPaths(setup.OutputDir);

        CleanupFile(paths.CopilotFile);
        CleanupDir(paths.AgentsDir);
        CleanupDir(paths.InstructionsDir);
        CleanupDir(paths.SkillsDir);
        CleanupDir(paths.OldSkillsDir);
    }
}
