
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OpenAI;
using OpenAI.Responses;
using Shared;
using Shared.Extensions;
using System.ClientModel;
using System.Text;

#pragma warning disable OPENAI001

namespace ToolsVsMcpVsSkillsVsAIContextProviders;

public class McpScenario
{
    public async Task Run()
    {
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

        AIAgent agent = client
            .GetResponsesClient()
            .AsAIAgent(
                model: "gpt-6-luna",
                instructions: "You are a GitHub Expert",
                tools: toolsInGitHubMcp.Cast<AITool>().ToList()
            )
            .AsBuilder()
            .Use(ToolCallingMiddleware) //Middleware
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
}