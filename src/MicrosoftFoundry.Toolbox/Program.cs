using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Core;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OpenAI.Responses;
using Shared;
using System.Net.Http.Headers;
using System.Text;
using Shared.Extensions;

#pragma warning disable OPENAI001

Utils.Init("Microsoft Foundry Toolbox");

string projectEndpoint = SecretsManager.GetSecrets().MicrosoftFoundryEndpoint;
AzureCliCredential credential = new();

AIProjectClient projectClient = new(new Uri(projectEndpoint), credential);

string deploymentModel = "gpt-6-luna";
string toolboxName = "my-toolbox";

//Create a Toolbox and some tools
AgentToolboxes toolboxClient = projectClient.AgentAdministrationClient.GetAgentToolboxes();

MCPToolboxTool learnTool = new(serverLabel: "MicrosoftLearn")
{
    Name = "ms_learn",
    ServerUri = new Uri("https://learn.microsoft.com/api/mcp"),
    Description = "Search Microsoft documentation and retrieve documentation pages and code samples.",
    ToolCallApprovalPolicy = new McpToolCallApprovalPolicy(
        new DefaultMcpToolCallApprovalPolicy("never"))
};

MCPToolboxTool trelloDotNetTool = new(serverLabel: "TrelloDotNet")
{
    Name = "trello_dot_net_docs",
    ServerUri = new Uri("https://trellodotnetassistantbackend.azurewebsites.net/runtime/webhooks/mcp?code=Tools"),
    Description = "Answer Question about C# OpenSource Project TrelloDotNet",
    ToolCallApprovalPolicy = new McpToolCallApprovalPolicy(
        new DefaultMcpToolCallApprovalPolicy("never"))
};

WebSearchToolboxTool webSearchToolboxTool = new WebSearchToolboxTool
{
    Name = "web_search"
};

ToolSearchToolboxTool searchToolboxTool = new ToolSearchToolboxTool
{
    Name = "toolbox_search"
};

ToolboxVersion toolboxVersion = await toolboxClient.CreateVersionAsync(
    name: toolboxName,
    tools: [
        learnTool,
        trelloDotNetTool,
        webSearchToolboxTool,
        searchToolboxTool
    ],
    description: "Toolbox with various Tools");

Utils.Green("Toolbox Created/Updated");

Uri toolboxEndpoint = new($"{projectEndpoint}/toolboxes/{toolboxName}/versions/{toolboxVersion.Version}/mcp?api-version=v1");

using HttpClient httpClient = new(new FoundryBearerTokenHandler(credential));

await using McpClient toolboxMcpClient = await McpClient.CreateAsync(new HttpClientTransport(
    new HttpClientTransportOptions
    {
        TransportMode = HttpTransportMode.StreamableHttp,
        Endpoint = toolboxEndpoint
    }, httpClient));

IList<McpClientTool> toolboxTools = await toolboxMcpClient.ListToolsAsync();
Console.WriteLine("Tools in Toolbox");
foreach (McpClientTool tool in toolboxTools)
{
    Utils.Gray($"- {tool.Name}");
}

//Regular Agent part
AIAgent agent = projectClient.ProjectOpenAIClient.GetResponsesClient().AsAIAgent(
        model: deploymentModel,
        instructions:$"Today's date is {DateTime.Today.ToLongDateString()}. Use only your tools and not own knowledge",
        tools: toolboxTools.Cast<AITool>().ToList())
    .AsBuilder()
    .Use(ToolCallingMiddleware)
    .Build();

AgentSession session = await agent.CreateSessionAsync();

while (true)
{
    Console.Write("> ");
    string input = Console.ReadLine() ?? "";
    if (!string.IsNullOrWhiteSpace(input))
    {
        AgentResponse response = await agent.RunAsync(input, session);
        Console.WriteLine(response);
        response.Usage.OutputAsInformation();
    }

    Utils.Separator();
}

async ValueTask<object?> ToolCallingMiddleware(AIAgent callingAgent, FunctionInvocationContext context, Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next, CancellationToken cancellationToken)
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

// Refresh the Entra token for every MCP HTTP request, including requests in long-running sessions.
internal sealed class FoundryBearerTokenHandler(TokenCredential credential) : HttpClientHandler
{
    private readonly TokenRequestContext _tokenContext = new(["https://ai.azure.com/.default"]);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        AccessToken token = await credential.GetTokenAsync(_tokenContext, cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        return await base.SendAsync(request, cancellationToken);
    }
}