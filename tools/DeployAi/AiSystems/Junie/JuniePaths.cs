using CreativeCoders.Core.IO;

namespace DeployAi.AiSystems.Junie;

public class JuniePaths(string outputDir)
{
    private const string JunieRootDir = ".junie";

    public string Root { get; } = FileSys.Path.Combine(outputDir, JunieRootDir);

    public string GuidelinesFile { get; set; } = FileSys.Path.Combine(outputDir, JunieRootDir, "guidelines.md");

    public string AgentsFile { get; } = FileSys.Path.Combine(outputDir, "AGENTS.md");
}
