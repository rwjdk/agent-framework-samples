using AgentFrameworkToolkit.AzureOpenAI;
using AgentFrameworkToolkit.OpenAI;
using Microsoft.Agents.AI;
using Shared;
using WhenToUseSkillsAndWhenToUseTools;

Secrets secrets = SecretsManager.GetSecrets();
AzureOpenAIConnection connection = new AzureOpenAIConnection
{
    DefaultClientType = ClientType.ResponsesApi,
    Endpoint = secrets.AzureOpenAiEndpoint,
    ApiKey = secrets.AzureOpenAiKey
};

AzureOpenAIAgentFactory agentFactory = new AzureOpenAIAgentFactory(connection);
agentFactory.Connection.DefaultClientType = global::AgentFrameworkToolkit.OpenAI.ClientType.ResponsesApi;

await SkillsImplementation.Run(agentFactory);
//await ToolsImplementation.Run(agentFactory);


public static class WeatherStuff
{
    public static string GetWeatherForCity(string city)
    {
        return "It is sunny and 19 Degrees";
    }
}


