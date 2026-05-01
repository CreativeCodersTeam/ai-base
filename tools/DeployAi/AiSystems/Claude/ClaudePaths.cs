using CreativeCoders.Core.IO;

namespace DeployAi.AiSystems.Claude;

public class ClaudePaths(string outputDir)
{
    private const string ClaudeRootDir = ".claude";

    public string Root => FileSys.Path.Combine(outputDir, ClaudeRootDir);

    public string ClaudeMdFile { get; } = FileSys.Path.Combine(outputDir, "CLAUDE.md");

    public string SkillsDir { get; } = FileSys.Path.Combine(outputDir, ClaudeRootDir, "skills");

    public string AgentsDir { get; } = FileSys.Path.Combine(outputDir, ClaudeRootDir, "agents");

    public string RulesDir { get; } = FileSys.Path.Combine(outputDir, ClaudeRootDir, "rules");
}
