#pragma warning disable OPENAI001
using OpenAI.Responses;
using Microsoft.Extensions.AI;
/* Steps:
 * 1: Create a 'HuggingFace Account' and generate an API Key (Access Token): https://huggingface.co/settings/tokens
 * 2: Add Nuget Packages (OpenAI + Microsoft.Agents.AI.OpenAI)
 * 3: Create an OpenAIClient with endpoint https://router.huggingface.co/v1
 * 4: Get a IChatClient and Create an AI Agent from it
 * 5: Call RunAsync or RunStreamingAsync
 */

using System.ClientModel;
using Microsoft.Agents.AI;
using OpenAI;
using OpenAI.Chat;

const string apiKey = "<YourAPIKey>";
const string model = "<yourModelName>"; //Example: Qwen/Qwen3-Coder-30B-A3B-Instruct

OpenAIClient client = new(new ApiKeyCredential(apiKey), new OpenAIClientOptions()
{
    Endpoint = new Uri("https://router.huggingface.co/v1")
});
ChatClientAgent agent = client.GetResponsesClient().AsIChatClient(model).AsAIAgent();

AgentResponse response = await agent.RunAsync("What is the Capital of Peru?");
Console.WriteLine(response);

Console.WriteLine("---");

await foreach (AgentResponseUpdate update in agent.RunStreamingAsync("How to make soup?"))
{
    Console.Write(update);
}