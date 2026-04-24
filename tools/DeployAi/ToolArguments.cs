namespace DeployAi;

public class ToolArguments
{
    public string OutputDir { get; set; } = string.Empty;

    public IEnumerable<string> AiSystems { get; set; } = [];

    public IEnumerable<string> Languages { get; set; } = [];

    public bool CleanupBeforeDeployment { get; set; }

    public bool PreferAgentsMd { get; set; }

    public string ProjectMarkdownFile { get; set; } = string.Empty;
}
