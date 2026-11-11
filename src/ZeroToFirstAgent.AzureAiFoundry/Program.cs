using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Responses;

#pragma warning disable OPENAI001

AIProjectClient projectClient = new(new Uri("<Azure AI Foundry project endpoint>"), new AzureCliCredential());

ChatClientAgent agent = projectClient.ProjectOpenAIClient.GetResponsesClient().AsAIAgent(
    model: "gpt-6-luna", instructions: "You are a nice AI");
AgentSession session = await agent.CreateSessionAsync();
Console.WriteLine(await agent.RunAsync("What is the capital of France?", session));
Console.WriteLine("---");
await foreach (AgentResponseUpdate update in agent.RunStreamingAsync("How to make soup?", session))
{
    Console.Write(update);
}
