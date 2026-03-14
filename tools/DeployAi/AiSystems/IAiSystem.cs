namespace DeployAi.AiSystems;

public interface IAiSystem
{
    string DisplayName { get; }

    string Name { get; }

    Task DeployAsync(DeploymentSetup setup);

    Task CleanupAsync(DeploymentSetup setup);
}
