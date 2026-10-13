using AgentFrameworkToolkit.AzureOpenAI;
using AgentFrameworkToolkit.OpenAI;
using Microsoft.Agents.AI;
using Shared;
using WhenToUseSkillsAndWhenToUseTools;

Secrets secrets = SecretsManager.GetSecrets();
AzureOpenAIConnection connection = new AzureOpenAIConnection
{
    Endpoint = secrets.AzureOpenAiEndpoint,
    ApiKey = secrets.AzureOpenAiKey,
    DefaultClientType = ClientType.ResponsesApi
};

AzureOpenAIAgentFactory agentFactory = new AzureOpenAIAgentFactory(connection);

//await SkillsImplementation.Run(agentFactory);
await ToolsImplementation.Run(agentFactory);


public static class WeatherStuff
{
    public static string GetWeatherForCity(string city)
    {
        return "It is sunny and 19 Degrees";
    }
}


