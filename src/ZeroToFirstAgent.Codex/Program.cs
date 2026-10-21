using AgentFrameworkToolkit.Codex;
using Microsoft.Agents.AI;
using Shared;

#pragma warning disable AFT999 //Experimental for now

Utils.Init("Codex in MAF - ZeroToFirstAgent");

//NB: You need to install and login to Codex CLI for this to work

CodexAgentFactory agentFactory = new();

CodexAgent agent = agentFactory.CreateAgent(Environment.CurrentDirectory);

const string q1 = "What is the capital of France?";
Utils.Green($"Normal: {q1}");
AgentResponse response1 = await agent.RunAsync(q1);
Console.WriteLine(response1);

Utils.Separator();

const string q2 = "The top movie on IMDB";
Utils.Green($"Structure Output: {q2}");
AgentResponse<Movie> response2 = await agent.RunAsync<Movie>(q2);
Console.WriteLine($"Title: {response2.Result.Title}");
Console.WriteLine($"Year of Release: {response2.Result.YearOfRelease}");

Utils.Separator();

const string q3 = "How to make Soup?";
Utils.Green($"Streaming: {q3}");
await foreach (AgentResponseUpdate update in agent.RunStreamingAsync(q3))
{
    Console.Write(update);
}


record Movie(string Title, int YearOfRelease);