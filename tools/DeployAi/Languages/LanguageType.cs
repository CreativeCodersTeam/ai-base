namespace DeployAi.Languages;

public class LanguageType(string name, string displayName)
{
    public string Name { get; } = name;

    public string DisplayName { get; } = displayName;
}
