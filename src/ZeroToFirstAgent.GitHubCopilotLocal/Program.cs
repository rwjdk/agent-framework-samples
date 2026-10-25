/* Steps:
 * 1: Login into GH or get a Token
 * 2: Add NuGet package Microsoft.Agents.AI.GitHub.Copilot.
 * 3: Create and start a CopilotClient, then call AsAIAgent with your model.
 * 4: Call RunAsync or RunStreamingAsync.
 */

using GitHub.Copilot;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Shared;

Utils.Init("Zero To FirstAgent - GitHub Copilot (Local)");

string gitHubToken = SecretsManager.GetSecrets().GitHubPatToken;

await using CopilotClient client = new CopilotClient(new CopilotClientOptions
{
    UseLoggedInUser = false, //set to true to use gh.exe login instead
    GitHubToken = gitHubToken

});
await client.StartAsync();

AIAgent agent = client.AsAIAgent(new SessionConfig
{
    WorkingDirectory = Environment.CurrentDirectory,
    Model = "gpt-6.1-sol",
    OnPermissionRequest = PermissionHandler.ApproveAll,
    Tools = [AIFunctionFactory.Create(GetWeather, "get_weather")]
});

const string q1 = "What is the capital of France - and how is the weather in its capital (use tool 'get_weather')?";
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

static string GetWeather(string city)
{
    return "Sunny and 19 degrees";
}

