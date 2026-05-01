using Spectre.Console;

namespace DeployAi.AiSystems.Claude;

public class ClaudeAiSystem() : AiSystemBase("claude", "Claude")
{
    public override void Deploy(DeploymentSetup setup)
    {
        AnsiConsole.WriteLine(
            $"Deploying {DisplayName} with the following languages: {string.Join(", ", setup.LanguageTypes.Select(l => l.DisplayName))}");

        var paths = new ClaudePaths(setup.OutputDir);

        CopyFile(GetGeneralInstructionFilePath(setup.SourceBaseDir), paths.ClaudeMdFile);
        //WriteFile(paths.ClaudeMdFile, CombineInstructionFiles(setup));

        CopyAgentFiles(setup, paths.AgentsDir);

        CopySkillFiles(setup, paths.SkillsDir);

        CopyInstructionFiles(setup, paths.RulesDir);
    }

    public override void Cleanup(DeploymentSetup setup)
    {
        AnsiConsole.WriteLine($"Cleaning up {DisplayName}...");

        var paths = new ClaudePaths(setup.OutputDir);

        CleanupFile(paths.ClaudeMdFile);
        CleanupDir(paths.AgentsDir);
        CleanupDir(paths.SkillsDir);
        CleanupDir(paths.RulesDir);
    }

    protected override string TransformInstructionContent(string content)
        => ClaudeRulesFrontmatter.Convert(content);

    protected override string TransformInstructionFileName(string fileName)
        => fileName.Replace(".instructions.", ".");
}
