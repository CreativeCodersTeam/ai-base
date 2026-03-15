using DeployAi.Languages;

namespace DeployAi;

public class DeploymentSetup
{
    public required string OutputDir { get; init; }

    public required string SourceBaseDir { get; init; }

    public bool PreferAgentsMd { get; init; }

    public required IEnumerable<LanguageType> LanguageTypes { get; init; }

    public required LanguageType General { get; init; }

    public IEnumerable<LanguageType> GetAllLanguageTypes()
    {
        return LanguageTypes.Concat([General]);
    }
}
