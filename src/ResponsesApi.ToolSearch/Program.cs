using System.ClientModel.Primitives;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OpenAI;
using Shared;
using Shared.Extensions;
using System.Text;
using System.Text.Json;
using OpenAI.Responses;
using OpenAIResponsesApi.ToolSearch;
// ReSharper disable UnreachableSwitchCaseDueToIntegerAnalysis

#pragma warning disable MEAI001
#pragma warning disable OPENAI001

Utils.Init("Tool Search");
ToolStrategy toolStrategy = ToolStrategy.WithSearchToolsInMultipleNamespaces;

Secrets secrets = SecretsManager.GetSecrets();

OpenAIClient client = ClientHelper.GetAzureOpenAIClient();

await using McpClient gitHubMcpClient = await McpClient.CreateAsync(new HttpClientTransport(new HttpClientTransportOptions
{
    TransportMode = HttpTransportMode.StreamableHttp,
    Endpoint = new Uri("https://api.githubcopilot.com/mcp/"),
    AdditionalHeaders = new Dictionary<string, string>
    {
        { "Authorization", secrets.GitHubPatToken }
    }
}));

IList<McpClientTool> toolsInGitHubMcp = await gitHubMcpClient.ListToolsAsync();
AIAgent agent;
switch (toolStrategy)
{
    case ToolStrategy.WithoutSearchTool:
        agent = GetAgentHelper.WithoutSearchTool(client, toolsInGitHubMcp);
        break;
    case ToolStrategy.WithSearchTool:
        agent = GetAgentHelper.WithSearchTool(client, toolsInGitHubMcp);
        break;
    case ToolStrategy.WithSearchToolsInOneNameSpace:
        agent = GetAgentHelper.WithSearchToolsInOneNameSpace(client, toolsInGitHubMcp);
        break;
    case ToolStrategy.WithSearchToolsInMultipleNamespaces:
        agent = GetAgentHelper.WithSearchToolsInMultipleNamespaces(client, toolsInGitHubMcp);
        break;
    default:
        throw new ArgumentOutOfRangeException(nameof(toolStrategy), toolStrategy, "Unknown tool strategy.");
}
agent = agent.AsBuilder().Use(FunctionCallMiddleware).Build();

AgentSession session = await agent.CreateSessionAsync();

while (true)
{
    Console.Write("> ");
    string? input = Console.ReadLine();
    AgentResponse response = await agent.RunAsync(input!, session);

    // Hosted tool search runs on the service, so function middleware does not see it.
    foreach (AIContent content in response.Messages.SelectMany(message => message.Contents))
    {
        if (content.RawRepresentation is not ResponseItem item)
        {
            continue;
        }

        using JsonDocument document = JsonDocument.Parse(ModelReaderWriter.Write(item).ToString());
        JsonElement root = document.RootElement;
        if (root.GetProperty("type").GetString() == "tool_search_call")
        {
            Utils.Gray($"- Tool Search: {root.GetProperty("arguments")}");
        }
        else if (root.GetProperty("type").GetString() == "tool_search_output")
        {
            Utils.Gray("- Tool Search Result:");
            Utils.Gray(JsonSerializer.Serialize(root.GetProperty("tools"), new JsonSerializerOptions
            {
                WriteIndented = true
            }));
        }
    }

    Console.WriteLine(response);
    response.Usage.OutputAsInformation();

    Utils.Separator();
}

async ValueTask<object?> FunctionCallMiddleware(AIAgent callingAgent, FunctionInvocationContext context, Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next, CancellationToken cancellationToken)
{
    StringBuilder functionCallDetails = new();
    functionCallDetails.Append($"- Tool Call: '{context.Function.Name}'");
    if (context.Arguments.Count > 0)
    {
        functionCallDetails.Append($" (Args: {string.Join(",", context.Arguments.Select(x => $"[{x.Key} = {x.Value}]"))}");
    }

    Utils.Gray(functionCallDetails.ToString());

    return await next(context, cancellationToken);
}