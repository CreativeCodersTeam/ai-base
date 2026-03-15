namespace DeployAi.AiSystems;

public interface IAiSystem
{
    string DisplayName { get; }

    string Name { get; }

    void Deploy(DeploymentSetup setup);

    void Cleanup(DeploymentSetup setup);
}
