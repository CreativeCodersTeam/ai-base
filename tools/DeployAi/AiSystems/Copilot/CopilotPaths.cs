using CreativeCoders.Core.IO;

namespace DeployAi.AiSystems.Copilot;

public class CopilotPaths(string outputDir)
{
    private const string GitHubRootDir = ".github";

    public string Root { get; } = FileSys.Path.Combine(outputDir, GitHubRootDir);

    public string CopilotFile { get; } = FileSys.Path.Combine(outputDir, GitHubRootDir, "copilot-instructions.md");

    public string SkillsDir { get; } = FileSys.Path.Combine(outputDir, GitHubRootDir, "skills");

    public string AgentsDir { get; } = FileSys.Path.Combine(outputDir, GitHubRootDir, "agents");

    public string InstructionsDir { get; } = FileSys.Path.Combine(outputDir, GitHubRootDir, "instructions");
}
