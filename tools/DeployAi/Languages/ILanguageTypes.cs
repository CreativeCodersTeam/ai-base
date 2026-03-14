namespace DeployAi.Languages;

public interface ILanguageTypes
{
    IEnumerable<LanguageType> GetLanguageTypes();

    public LanguageType General { get; }
}
