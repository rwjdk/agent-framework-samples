//YouTube video that cover this sample
//- Basic: https://youtu.be/gJTodKpv8Ik
//- Advanced: https://youtu.be/dCtojrK8bKk
//- MCP: https://youtu.be/Y5IKdt9vdJM


using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using Shared;
using System.ClientModel;
using System.Text;
using ModelContextProtocol.Client;
using OpenAI.Chat;
using OpenAI.Responses;
using Shared.Extensions;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;
#pragma warning disable MEAI001
#pragma warning disable OPENAI001

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

IEnumerable<AITool> aiTools = toolsInGitHubMcp.Cast<AITool>();
HostedToolSearchTool searchTool = new HostedToolSearchTool
{
    Namespace = "github",
    NamespaceDescription =
        "GitHub tools for searching repositories and code, reading files, " +
        "managing issues and pull requests, reviewing changes, " +
        "and inspecting branches, commits, and workflow runs."
};
AIAgent agent = client
    .GetResponsesClient()
    .AsAIAgent(
        instructions: "You are a GitHub Expert",
        tools: [..aiTools.ToList(),searchTool]
    , model: "gpt-6-luna")
    .AsBuilder()
    .Use(FunctionCallMiddleware) //Middleware
    .Build();

AgentSession session = await agent.CreateSessionAsync();

while (true)
{
    Console.Write("> ");
    string? input = Console.ReadLine();
    ChatMessage message = new(ChatRole.User, input);
    AgentResponse response = await agent.RunAsync(message, session);

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