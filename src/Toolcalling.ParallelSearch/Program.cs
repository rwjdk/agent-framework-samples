using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using OpenAI;
using Toolcalling.ParallelSearch;

bool check = args is ["--check"];
if (args.Length > 0 && !check)
{
    Console.Error.WriteLine("Usage: dotnet run -- [--check]");
    return 1;
}

string? apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
if (!check && string.IsNullOrWhiteSpace(apiKey))
{
    Console.Error.WriteLine("Set OPENAI_API_KEY for the agent, or use --check to test MCP without a model.");
    return 1;
}

using CancellationTokenSource cancellation = new();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cancellation.Cancel();
};
CancellationToken token = cancellation.Token;

await using McpClient mcp = await ParallelSearch.ConnectAsync(token);
IList<McpClientTool> tools = await mcp.ListToolsAsync(cancellationToken: token);

if (check)
{
    // Invoke the discovered functions directly; no model or credentials are needed.
    McpClientTool search = tools.Single(tool => tool.Name == "web_search");
    CallToolResult searchResult = await search.CallAsync(new Dictionary<string, object?>
    {
        ["objective"] = "Find the Microsoft Agent Framework documentation.",
        ["search_queries"] = new[] { "Microsoft Agent Framework documentation" }
    }, cancellationToken: token);
    PrintResult(searchResult);
    if (searchResult.IsError == true) return 1;

    McpClientTool fetch = tools.Single(tool => tool.Name == "web_fetch");
    CallToolResult fetchResult = await fetch.CallAsync(new Dictionary<string, object?>
    {
        ["urls"] = new[] { "https://learn.microsoft.com/en-us/agent-framework/overview/" },
        ["objective"] = "What is Microsoft Agent Framework?"
    }, cancellationToken: token);
    PrintResult(fetchResult);
    return fetchResult.IsError == true ? 1 : 0;
}

string model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4.1";
using IChatClient chatClient = new OpenAIClient(apiKey!).GetChatClient(model).AsIChatClient();
AIAgent agent = ParallelSearch.CreateAgent(chatClient, tools);
AgentSession session = await agent.CreateSessionAsync(token);

while (!token.IsCancellationRequested)
{
    Console.Write("> ");
    string? input = Console.ReadLine();
    if (input is null || string.Equals(input, "exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }
    if (!string.IsNullOrWhiteSpace(input))
    {
        Console.WriteLine(await agent.RunAsync(input, session, cancellationToken: token));
    }
}
return 0;

static void PrintResult(CallToolResult result)
{
    foreach (TextContentBlock content in result.Content.OfType<TextContentBlock>())
    {
        Console.WriteLine(content.Text);
    }
}
