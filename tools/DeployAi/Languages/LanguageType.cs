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
            return Array.Empty<string>();
        }

        return FileSys.Directory.EnumerateFiles(GetAgentsDir(setup), "*.md");
    }

    public IEnumerable<string> GetInstructionFiles(DeploymentSetup setup)
    {
        var instructionsDir = GetInstructionsDir(setup);

        if (!FileSys.Directory.Exists(instructionsDir))
        {
            return Array.Empty<string>();
        }

        return FileSys.Directory.EnumerateFiles(instructionsDir, "*.instructions.md");
    }

    public IEnumerable<LanguageSkill> GetSkills(DeploymentSetup setup)
    {
        var skillsDir = GetSkillsDir(setup);

        if (!FileSys.Directory.Exists(skillsDir))
        {
            return Array.Empty<LanguageSkill>();
        }

        return from skillDir in FileSys.Directory.EnumerateDirectories(skillsDir)
            let skillName = FileSys.Path.GetFileName(skillDir)
            let skillFile = FileSys.Path.Combine(skillDir, "SKILL.md")
            where FileSys.File.Exists(skillFile)
            select new LanguageSkill(skillName, skillFile);
    }
}
