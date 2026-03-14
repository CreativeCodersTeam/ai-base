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

        var files = FileSys.Directory.GetFiles(sourceDir, "*.md", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            AnsiConsole.WriteLine($"Copying agent file '{file}'...");
            FileSys.File.Copy(file, FileSys.Path.Combine(targetDir, FileSys.Path.GetFileName(file)), true);
        }
    }

    protected async Task CopyInstructionFilesAsync(string sourceDir, string targetDir)
    {
        AnsiConsole.WriteLine($"Copying instructions from {sourceDir} to {targetDir}...");

        var files = FileSys.Directory.GetFiles(sourceDir, "*.instructions.md", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            // AnsiConsole.WriteLine($"Copying agent file '{file}'...");
            // FileSys.File.Copy(file, FileSys.Path.Combine(targetDir, FileSys.Path.GetFileName(file)), true);
        }
    }

    protected async Task CopyFileAsync(string sourceFile, string targetFile)
    {
        AnsiConsole.WriteLine($"Copying file from {sourceFile} to {targetFile}...");

        FileSys.File.Copy(sourceFile, targetFile, true);
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

    protected string GetDirPath(string baseDir, string pathKind, string langName)
    {
        return FileSys.Path.Combine(baseDir, "src", pathKind, langName);
    }

    protected string GetGeneralInstructionFilePath(string baseDir)
    {
        return FileSys.Path.Combine(GetDirPath(baseDir, "instructions", "general"), "general.instructions.md");
    }

    protected string GetAgentsSourceDir(string baseDir)
    {
        return FileSys.Path.Combine(baseDir, "src", "agents");
    }

    protected string GetSkillsSourceDir(string baseDir)
    {
        return FileSys.Path.Combine(baseDir, "src", "skills");
    }

    protected string GetInstructionsSourceDir(string baseDir)
    {
        return FileSys.Path.Combine(baseDir, "src", "instructions");
    }
}
