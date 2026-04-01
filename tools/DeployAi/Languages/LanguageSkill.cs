namespace DeployAi.Languages;

public class LanguageSkill(string name, string fileName)
{
    public string Name { get; } = name;

    public string FileName { get; } = fileName;

    public IEnumerable<string> AdditionalFiles { get; init; } = [];
}
