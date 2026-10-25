/* Steps:
 * 1: Install Copilot CLI: winget install GitHub.Copilot
 * 2: Run copilot and enter /login to sign in with your Copilot account.
 * 3: Add NuGet package Microsoft.Agents.AI.GitHub.Copilot.
 * 4: Create and start a CopilotClient, then call AsAIAgent with your model.
 * 5: Call RunAsync or RunStreamingAsync.
 */

using GitHub.Copilot;
using Microsoft.Agents.AI;
using Shared;

Utils.Init("Zero To FirstAgent - GitHub Copilot (Local)");

await using CopilotClient client = new();
await client.StartAsync();

AIAgent agent = client.AsAIAgent(new SessionConfig
{
    Model = "gpt-6.1-sol"
});

const string q1 = "What is the capital of France?";
Utils.Green($"Normal: {q1}");
AgentResponse response1 = await agent.RunAsync(q1);
Console.WriteLine(response1);

Utils.Separator();

const string q2 = "How to make soup?";
Utils.Green($"Streaming: {q2}");
await foreach (AgentResponseUpdate update in agent.RunStreamingAsync(q2))
{
    Console.Write(update);
}

