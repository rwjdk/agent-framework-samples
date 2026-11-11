#pragma warning disable OPENAI001
using OpenAI.Responses;
using Microsoft.Extensions.AI;
/* Steps:
 * 1: Get a Cohere API Key (https://cloud.cerebras.ai/)
 * 2: Add Nuget Package (Microsoft.Agents.AI.OpenAI)
 * 3: Create an OpenAIClient with endpoint https://api.cerebras.ai/v1
 * 4: Get a IChatClient and Create an AI Agent from it
 * 5: Call RunAsync or RunStreamingAsync
 */

using System.ClientModel;
using Microsoft.Agents.AI;
using OpenAI;
using OpenAI.Chat;

const string apiKey = "<API Key>";
const string model = "command-a-03-2025";

OpenAIClient client = new(new ApiKeyCredential(apiKey), new OpenAIClientOptions
{
    Endpoint = new Uri("https://api.cohere.ai/compatibility/v1")
});
ChatClientAgent agent = client.GetResponsesClient().AsIChatClient(model).AsAIAgent();

AgentResponse response = await agent.RunAsync("What is the Capital of Finland?");
Console.WriteLine(response);

Console.WriteLine("---");

await foreach (AgentResponseUpdate update in agent.RunStreamingAsync("How to make soup?"))
{
    Console.Write(update);
}