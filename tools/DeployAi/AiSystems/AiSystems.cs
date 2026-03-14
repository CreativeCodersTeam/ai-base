using DeployAi.AiSystems.Claude;
using DeployAi.AiSystems.Copilot;
using DeployAi.AiSystems.Junie;

namespace DeployAi.AiSystems;

public class AiSystems : IAiSystems
{
    private readonly IEnumerable<IAiSystem> _aiSystems =
    [
        new ClaudeAiSystem(),
        new CopilotAiSystem(),
        new JunieAiSystem()
    ];

    public IEnumerable<IAiSystem> GetAiSystems() => _aiSystems;

    public IAiSystem? GetAiSystem(string name)
    {
        return _aiSystems.FirstOrDefault(x => x.Name == name);
    }
}
