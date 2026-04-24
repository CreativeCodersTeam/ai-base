using CreativeCoders.Core.IO;
using Spectre.Console;

namespace DeployAi.AiSystems.Copilot;

public class CopilotAiSystem() : AiSystemBase("copilot", "Copilot")
{
    public override void Deploy(DeploymentSetup setup)
    {
        AnsiConsole.WriteLine(
            $"Deploying {DisplayName} with the following languages: {string.Join(", ", setup.LanguageTypes.Select(l => l.DisplayName))}");

        var paths = new CopilotPaths(setup.OutputDir);

        var generalFilePath = GetGeneralInstructionFilePath(setup.SourceBaseDir);

        if (!string.IsNullOrEmpty(setup.ProjectMarkdownFile) && FileSys.File.Exists(setup.ProjectMarkdownFile))
        {
            var content = FileSys.File.ReadAllText(generalFilePath)
                          + Environment.NewLine
                          + FileSys.File.ReadAllText(setup.ProjectMarkdownFile);
            WriteFile(paths.CopilotFile, content);
        }
        else
        {
            CopyFile(generalFilePath, paths.CopilotFile);
        }

        CopyAgentFiles(setup, paths.AgentsDir);

        CopyInstructionFiles(setup, paths.InstructionsDir);

        CopySkillFiles(setup, paths.OldSkillsDir);
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
