using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Responses;
using Shared;
#pragma warning disable OPENAI001

namespace AgentFrameworkToolkit.AsAIAgentComparison;

public static class AgentFrameworkAlone
{
    public static async Task RunAsync(string prompt)
    {
        var client = ClientHelper.GetAzureOpenAIClient();
        ChatClientAgent agent = client.GetResponsesClient().AsAIAgent(
            model: "gpt-5-mini",
            options: new ChatClientAgentOptions
            {
                ChatOptions = new ChatOptions
                {
                    RawRepresentationFactory = _ => new CreateResponseOptions
                    {
                        ReasoningOptions = new ResponseReasoningOptions
                        {
                            ReasoningEffortLevel = ResponseReasoningEffortLevel.High,
                            ReasoningSummaryVerbosity = ResponseReasoningSummaryVerbosity.Detailed
                        }
                    }
                }
            });

        AgentResponse response = await agent.RunAsync(prompt);
        Console.WriteLine(response);
    }
}
