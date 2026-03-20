using System.Text;
using CreativeCoders.Core.IO;
using Spectre.Console;

namespace DeployAi.AiSystems;

public abstract class AiSystemBase(string name, string displayName) : IAiSystem
{
    public string DisplayName { get; } = displayName;

    public string Name { get; } = name;

    public abstract void Deploy(DeploymentSetup setup);

    public abstract void Cleanup(DeploymentSetup setup);

    protected void CopySkillFiles(DeploymentSetup setup, string targetDir)
    {
        AnsiConsole.WriteLine($"Copying skills for {DisplayName}...");

        var skills = setup.GetAllLanguageTypes().SelectMany(x => x.GetSkills(setup));

        foreach (var skill in skills)
        {
            CopyFile(skill.FileName, FileSys.Path.Combine(targetDir, skill.Name, "SKILL.md"));
        }
    }

    protected void CopyAgentFiles(DeploymentSetup setup, string targetDir)
    {
        AnsiConsole.WriteLine($"Copying agents for {DisplayName}...");

        var agentFiles = setup.GetAllLanguageTypes().SelectMany(x => x.GetAgentFiles(setup));

        foreach (var agentFile in agentFiles)
        {
            CopyFile(agentFile, FileSys.Path.Combine(targetDir, FileSys.Path.GetFileName(agentFile)));
        }
    }

    protected void CopyInstructionFiles(DeploymentSetup setup, string targetDir)
    {
        AnsiConsole.WriteLine($"Copying instructions for {DisplayName}...");

        var instructionFiles = setup.LanguageTypes.SelectMany(x => x.GetInstructionFiles(setup));

        foreach (var instructionFile in instructionFiles)
        {
            CopyFile(instructionFile, FileSys.Path.Combine(targetDir, FileSys.Path.GetFileName(instructionFile)));
        }
    }

    protected static void CopyFile(string sourceFile, string targetFile)
    {
        AnsiConsole.WriteLine($"Copying file from {sourceFile} to {targetFile}...");

        FileSys.Directory.CreateDirectory(FileSys.Path.GetDirectoryName(targetFile) ??
                                          throw new InvalidOperationException(
                                              $"Cannot get directory name from path '{targetFile}'"));

        FileSys.File.Copy(sourceFile, targetFile, true);
    }

    protected static void WriteFile(string filePath, string content)
    {
        AnsiConsole.WriteLine($"Writing file '{filePath}'...");

        FileSys.Directory.CreateDirectory(FileSys.Path.GetDirectoryName(filePath) ??
                                          throw new InvalidOperationException(
                                              $"Cannot get directory name from path '{filePath}'"));

        FileSys.File.WriteAllText(filePath, content);
    }

    protected static void CleanupFile(string filePath)
    {
        if (!FileSys.File.Exists(filePath))
        {
            return;
        }

        AnsiConsole.Write($"Cleaning up file '{filePath}' ...");

        FileSys.File.Delete(filePath);

        AnsiConsole.MarkupLine(" done.");
    }

    protected static void CleanupDir(string dirPath)
    {
        if (!FileSys.Directory.Exists(dirPath))
        {
            return;
        }

        AnsiConsole.Write($"Cleaning up directory '{dirPath}' ...");

        FileSys.Directory.Delete(dirPath, true);

        AnsiConsole.MarkupLine(" done.");
    }

    private static string GetDirPath(string baseDir, string pathKind, string langName)
    {
        return FileSys.Path.Combine(baseDir, "src", pathKind, langName);
    }

    protected static string GetGeneralInstructionFilePath(string baseDir)
    {
        return FileSys.Path.Combine(GetDirPath(baseDir, "instructions", "general"), "general.instructions.md");
    }

    protected static string CombineInstructionFiles(DeploymentSetup setup)
    {
        var files = setup.LanguageTypes.SelectMany(x => x.GetInstructionFiles(setup)).ToArray();

        var generalFile = GetGeneralInstructionFilePath(setup.SourceBaseDir);

        var contentBuilder = new StringBuilder();

        contentBuilder.AppendLine("-----------------------------------------------------------");
        contentBuilder.AppendLine(
            "GitHub Copilot must ignore the following content in this file, cause Copilot gets this infos from the files in the instructions:");
        contentBuilder.AppendLine();

        contentBuilder.AppendLine(FileSys.File.ReadAllText(generalFile));
        contentBuilder.AppendLine("-----------------------------------------------------------");
        contentBuilder.AppendLine();
        contentBuilder.AppendLine();

        foreach (var file in files)
        {
            contentBuilder.AppendLine(FileSys.File.ReadAllText(file));
            contentBuilder.AppendLine("-----------------------------------------------------------");
            contentBuilder.AppendLine();
            contentBuilder.AppendLine();
        }

        return contentBuilder.ToString();
    }
}
