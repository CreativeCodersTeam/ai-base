using CreativeCoders.Core.IO;

namespace DeployAi.Languages;

public class LanguageType(string name, string displayName)
{
    public string Name { get; } = name;

    public string DisplayName { get; } = displayName;

    private string GetAgentsDir(DeploymentSetup setup)
    {
        return FileSys.Path.Combine(setup.SourceBaseDir, "src", "agents", Name);
    }

    private string GetSkillsDir(DeploymentSetup setup)
    {
        return FileSys.Path.Combine(setup.SourceBaseDir, "src", "skills", Name);
    }

    private string GetInstructionsDir(DeploymentSetup setup)
    {
        return FileSys.Path.Combine(setup.SourceBaseDir, "src", "instructions", Name);
    }

    public IEnumerable<string> GetAgentFiles(DeploymentSetup setup)
    {
        var agentsDir = GetAgentsDir(setup);

        if (!FileSys.Directory.Exists(agentsDir))
        {
            return [];
        }

        return FileSys.Directory.EnumerateFiles(GetAgentsDir(setup), "*.md");
    }

    public IEnumerable<string> GetInstructionFiles(DeploymentSetup setup)
    {
        var instructionsDir = GetInstructionsDir(setup);

        return !FileSys.Directory.Exists(instructionsDir)
            ? []
            : FileSys.Directory.EnumerateFiles(instructionsDir, "*.instructions.md");
    }

    public IEnumerable<LanguageSkill> GetSkills(DeploymentSetup setup)
    {
        var skillsDir = GetSkillsDir(setup);

        if (!FileSys.Directory.Exists(skillsDir))
        {
            return [];
        }

        return from skillDir in FileSys.Directory.EnumerateDirectories(skillsDir)
            let skillName = FileSys.Path.GetFileName(skillDir)
            let skillFile = FileSys.Path.Combine(skillDir, "SKILL.md")
            where FileSys.File.Exists(skillFile)
            let ignoredRoots = FileSys.Directory
                .EnumerateFiles(skillDir, ".ai-ignore-files", SearchOption.AllDirectories)
                .Select(marker => FileSys.Path.GetDirectoryName(marker)!)
                .Select(dir => dir.TrimEnd(FileSys.Path.DirectorySeparatorChar, FileSys.Path.AltDirectorySeparatorChar))
                .ToArray()
            select new LanguageSkill(skillName, skillFile)
            {
                AdditionalFiles = FileSys.Directory
                    .EnumerateFiles(skillDir, "*", SearchOption.AllDirectories)
                    .Where(x => x != skillFile)
                    .Where(x => !IsUnderAnyRoot(x, ignoredRoots))
                    .ToArray()
            };
    }

    private static bool IsUnderAnyRoot(string filePath, string[] roots)
    {
        if (roots.Length == 0)
        {
            return false;
        }

        var dir = FileSys.Path.GetDirectoryName(filePath);
        if (dir is null)
        {
            return false;
        }

        var sep = FileSys.Path.DirectorySeparatorChar;
        foreach (var root in roots)
        {
            if (string.Equals(dir, root, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (dir.StartsWith(root + sep, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
