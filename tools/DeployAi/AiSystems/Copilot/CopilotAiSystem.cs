using CreativeCoders.Core.IO;
using Spectre.Console;

namespace DeployAi.AiSystems.Copilot;

public class CopilotAiSystem() : AiSystemBase("copilot", "Copilot")
{
    public override async Task DeployAsync(DeploymentSetup setup)
    {
        AnsiConsole.WriteLine(
            $"Deploying {DisplayName} with the following languages: {string.Join(", ", setup.LanguageTypes.Select(l => l.DisplayName))}");

        var paths = new CopilotPaths(setup.OutputDir);

        await CopyFileAsync(
            GetGeneralInstructionFilePath(setup.SourceBaseDir),
            paths.CopilotFile);

        await CopyAgentFilesAsync(
            GetAgentsSourceDir(setup.SourceBaseDir),
            paths.AgentsDir);

        await CopyInstructionFilesAsync(
            GetInstructionsSourceDir(setup.SourceBaseDir),
            paths.InstructionsDir);

        await CopySkillFilesAsync(
            GetSkillsSourceDir(setup.SourceBaseDir),
            paths.SkillsDir);
    }

    public override async Task CleanupAsync(DeploymentSetup setup)
    {
        AnsiConsole.WriteLine($"Cleaning up {DisplayName}...");

        var paths = new CopilotPaths(setup.OutputDir);

        await CleanupFileAsync(paths.CopilotFile);
        await CleanupDirAsync(paths.AgentsDir);
        await CleanupDirAsync(paths.InstructionsDir);
        await CleanupDirAsync(paths.SkillsDir);
    }
}
