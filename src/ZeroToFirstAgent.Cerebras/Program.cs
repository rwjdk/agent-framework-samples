#pragma warning disable OPENAI001
using OpenAI.Responses;
using Microsoft.Extensions.AI;
/* Steps:
 * 1: Get a Cerebras API Key (https://cloud.cerebras.ai/)
 * 2: Add Nuget Package (Microsoft.Agents.AI.OpenAI)
 * 3: Create an OpenAIClient with endpoint https://api.cerebras.ai/v1
 * 4: Get a IChatClient and Create an AI Agent from it
 * 5: Call RunAsync or RunStreamingAsync
 */

using System.ClientModel;
using Microsoft.Agents.AI;
using OpenAI;
using OpenAI.Chat;

const string apiKey = "<ApiKey>";
const string model = "qwen-3-235b-a22b-instruct-2507";

OpenAIClient client = new(new ApiKeyCredential(apiKey), new OpenAIClientOptions
{
    Endpoint = new Uri("https://api.cerebras.ai/v1")
});
ChatClientAgent agent = client.GetResponsesClient().AsIChatClient(model).AsAIAgent();

AgentResponse response = await agent.RunAsync("What is the Capital of Finland?");
Console.WriteLine(response);

Console.WriteLine("---");

await foreach (AgentResponseUpdate update in agent.RunStreamingAsync("How to make soup?"))
{
    Console.Write(update);
}