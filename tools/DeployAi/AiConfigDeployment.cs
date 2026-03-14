using CreativeCoders.Core.IO;
using CreativeCoders.SysConsole.Core;
using DeployAi.AiSystems;
using DeployAi.Languages;
using Spectre.Console;

namespace DeployAi;

public class AiConfigDeployment(IAiSystems aiSystems, ILanguageTypes languageTypes)
{
    public async Task DeployAsync(ToolArguments arguments)
    {
        var aiSystemsToDeploy = aiSystems.GetAiSystems()
            .Where(s => arguments.AiSystems.Contains(s.Name, StringComparer.OrdinalIgnoreCase))
            .ToArray();

        var languagesToDeploy = languageTypes.GetLanguageTypes()
            .Where(x => arguments.Languages.Contains(x.Name, StringComparer.OrdinalIgnoreCase))
            .ToArray();

        if (aiSystemsToDeploy.Length == 0)
        {
            AnsiConsole.WriteLine("No AI systems to deploy based on the configuration.");
            return;
        }

        var setup = new DeploymentSetup
        {
            OutputDir = FileSys.Path.GetFullPath(arguments.OutputDir),
            PreferAgentsMd = arguments.PreferAgentsMd,
            LanguageTypes = languagesToDeploy,
            General = languageTypes.General
        };

        AnsiConsole.WriteLine(
            $"Deployment setup: OutputDir={setup.OutputDir}, PreferAgentsMd={setup.PreferAgentsMd}, Languages={string.Join(", ", setup.LanguageTypes.Select(x => x.Name))}");

        if (arguments.CleanupBeforeDeployment)
        {
            AnsiConsole.WriteLine("Cleaning up existing deployments...");
            foreach (var aiSystem in aiSystemsToDeploy)
            {
                await aiSystem.CleanupAsync(setup);
            }

            AnsiConsole.MarkupLine("Cleanup completed.".ToSuccessMarkup());
        }

        AnsiConsole.WriteLine(
            $"Deploying {aiSystemsToDeploy.Length} AI system(s): {string.Join(", ", aiSystemsToDeploy.Select(s => s.DisplayName))}");

        foreach (var aiSystem in aiSystemsToDeploy)
        {
            AnsiConsole.WriteLine($"Deploying {aiSystem.DisplayName}...");
            await aiSystem.DeployAsync(setup);
            AnsiConsole.MarkupLine($"{aiSystem.DisplayName} deployed successfully.".ToSuccessMarkup());
        }
    }
}
