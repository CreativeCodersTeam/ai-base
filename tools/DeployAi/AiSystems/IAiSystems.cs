namespace DeployAi.AiSystems;

public interface IAiSystems
{
    IEnumerable<IAiSystem> GetAiSystems();

    IAiSystem? GetAiSystem(string name);
}
