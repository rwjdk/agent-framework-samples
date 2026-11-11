#pragma warning disable OPENAI001
using OpenAI.Responses;
using Microsoft.Extensions.AI;
//YouTube video that cover this sample: https://youtu.be/CvA69UyqJ7U

/* Steps:
 * 1: Create an 'OpenAI API Account'
 * 2: Add Nuget Packages (OpenAI + Microsoft.Agents.AI.OpenAI)
 * 3: Create an OpenAIClient
 * 4: Get a IChatClient and Create an AI Agent from it
 * 5: Call RunAsync or RunStreamingAsync
 */

using Microsoft.Agents.AI;
using OpenAI;
using OpenAI.Chat;

const string apiKey = "todo";
const string model = "todo";
OpenAIClient client = new OpenAIClient(apiKey);
ChatClientAgent agent = client.GetResponsesClient().AsIChatClient(model).AsAIAgent();

AgentResponse response = await agent.RunAsync("What is the Capital of Germany?");
Console.WriteLine(response);

Console.WriteLine("---");

await foreach (AgentResponseUpdate update in agent.RunStreamingAsync("How to make soup?"))
{
    Console.Write(update);
}