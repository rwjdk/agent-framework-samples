using AgentFrameworkToolkit.OpenAI;
using Microsoft.Agents.AI;
using Shared;

namespace AgentFrameworkToolkit.AsAIAgentComparison;

public static class AgentFrameworkToolkitExtensionMethod
{
    public static async Task RunAsync(string prompt)
    {
        // Keep the existing Azure OpenAI connection; only agent creation changes.
        var client = ClientHelper.GetAzureOpenAIClient();

        AIAgent agent = client.AsAIAgent(new AgentOptions
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
