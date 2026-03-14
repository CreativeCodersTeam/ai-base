using CreativeCoders.Core.IO;
using Spectre.Console;

namespace DeployAi.AiSystems;

public abstract class AiSystemBase(string name, string displayName) : IAiSystem
{
    public string DisplayName { get; } = displayName;

    public string Name { get; } = name;

    public abstract Task DeployAsync(DeploymentSetup setup);

    public abstract Task CleanupAsync(DeploymentSetup setup);

    protected async Task CopySkillFilesAsync(string sourceDir, string targetDir)
    {
        AnsiConsole.WriteLine($"Copying skills from {sourceDir} to {targetDir}...");
    }

    protected async Task CopyAgentFilesAsync(string sourceDir, string targetDir)
    {
        AnsiConsole.WriteLine($"Copying agents from {sourceDir} to {targetDir}...");
    }

    protected async Task CopyInstructionFilesAsync(string sourceDir, string targetDir)
    {
        AnsiConsole.WriteLine($"Copying instructions from {sourceDir} to {targetDir}...");
    }

    protected async Task CleanupFileAsync(string filePath)
    {
        if (!FileSys.File.Exists(filePath))
        {
            return;
        }

        AnsiConsole.Write($"Cleaning up file '{filePath}' ...");

        FileSys.File.Delete(filePath);

        AnsiConsole.MarkupLine(" done.");
    }

    protected async Task CleanupDirAsync(string dirPath)
    {
        if (!FileSys.Directory.Exists(dirPath))
        {
            return;
        }

        AnsiConsole.Write($"Cleaning up directory '{dirPath}' ...");

        FileSys.Directory.Delete(dirPath, true);

        AnsiConsole.MarkupLine(" done.");
    }
}
