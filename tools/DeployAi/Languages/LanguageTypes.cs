namespace DeployAi.Languages;

public class LanguageTypes : ILanguageTypes
{
    private readonly IEnumerable<LanguageType> _languageTypes =
    [
        new LanguageType("csharp", "C#"),
        new LanguageType("angular", "Angular"),
        new LanguageType("javascript", "JavaScript"),
        new LanguageType("typescript", "TypeScript"),
        new LanguageType("java", "Java"),
        new LanguageType("docker", "Docker"),
        new LanguageType("superpowers", "Superpowers")
    ];

    public IEnumerable<LanguageType> GetLanguageTypes() => _languageTypes;

    public LanguageType General { get; } = new LanguageType("general", "General");
}
