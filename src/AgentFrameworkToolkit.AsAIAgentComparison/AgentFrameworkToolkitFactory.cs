using AgentFrameworkToolkit.AzureOpenAI;
using AgentFrameworkToolkit.OpenAI;
using Microsoft.Agents.AI;
using Shared;

namespace AgentFrameworkToolkit.AsAIAgentComparison;

public static class AgentFrameworkToolkitFactory
{
    public static async Task RunAsync(string prompt)
    {
        Secrets secrets = SecretsManager.GetSecrets();
        AzureOpenAIAgentFactory factory = new(secrets.AzureOpenAiEndpoint, secrets.AzureOpenAiKey);

        AzureOpenAIAgent agent = factory.CreateAgent(new AgentOptions
        {
            ClientType = ClientType.ResponsesApi,
            Model = OpenAIChatModels.Gpt5Mini,
            ReasoningEffort = OpenAIReasoningEffort.High,
            ReasoningSummaryVerbosity = OpenAIReasoningSummaryVerbosity.Detailed
        });

        AgentResponse response = await agent.RunAsync(prompt);
        Console.WriteLine(response);
    }
}
