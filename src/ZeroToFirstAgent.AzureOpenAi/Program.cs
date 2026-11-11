//YouTube video that cover this sample:

/* Steps:
 * 1: Create an 'Azure AI Foundry' Resource (or legacy 'Azure OpenAI Resource') + Deploy Model
 * 2: Add Nuget Packages (Microsoft.Agents.AI.OpenAI)
 * 3: Create an OpenAIClient with Endpoint (API Key or Azure Identity)
 * 4: Get a GetResponsesClient and Create an AI Agent from it (model is mandatory)
 * 5: Call RunAsync or RunStreamingAsync
 */

using System.ClientModel;
using Microsoft.Agents.AI;
using OpenAI;
using OpenAI.Responses;

#pragma warning disable OPENAI001

const string endpoint = "todo";
const string apiKey = "todo";
const string model = "gpt-6-luna";
OpenAIClient client = new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions
{
    Endpoint = new Uri(endpoint) // Endpoint need to end in /openai/v1
});
ChatClientAgent agent = client.GetResponsesClient().AsAIAgent(model: model);

//Simple Response
AgentResponse response = await agent.RunAsync("What is the capital of France?");
Console.WriteLine(response);

Console.WriteLine("---");

//Streaming Result
await foreach (AgentResponseUpdate update in agent.RunStreamingAsync("How to make soup?"))
{
    Console.Write(update);
}