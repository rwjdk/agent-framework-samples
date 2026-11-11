/* Steps:
 * 1: Create an 'Azure AI Foundry' Resource (or legacy 'Azure OpenAI Resource') + Deploy Model
 * 2: Add Nuget Packages (Microsoft.Agents.AI.AzureAI)
 * 3: Create an AIProjectClient with Endpoint and AzureCli
 * 4: Create an AI Agent from the client (instructions + model are mandatory)
 * 5: Call RunAsync or RunStreamingAsync
 */

using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry;

#pragma warning disable OPENAI001

string url = "todo";
string modelToUse = "gpt-6-luna";

AzureCliCredential credential = new AzureCliCredential();
AIProjectClient client = new AIProjectClient(new Uri(url), credential);

AIAgent agent = client.AsAIAgent(model: modelToUse, instructions: "You are a nice AI");

//Simple Response
AgentResponse response = await agent.RunAsync("What is the capital of France?");
Console.WriteLine(response);

Console.WriteLine("---");

//Streaming Result
await foreach (AgentResponseUpdate update in agent.RunStreamingAsync("How to make soup?"))
{
    Console.Write(update);
}