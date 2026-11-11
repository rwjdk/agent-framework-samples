using System.Text;
using AIContextProvider.Mem0.Mem0Provider;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Responses;
using Shared;
#pragma warning disable OPENAI001

Utils.Init("Mem0 Provider");
Secrets secrets = SecretsManager.GetSecrets();
OpenAIClient client = ClientHelper.GetAzureOpenAIClient();

using Mem0Client mem0Client = new(secrets.Mem0ApiKey);

AIAgent agent = client
    .GetResponsesClient()
    .AsAIAgent(new ChatClientAgentOptions
    {
        ChatOptions = new ChatOptions
        {
            Instructions = """
                           You are a helpful assistant. Use relevant user memories when useful.
                           When the user corrects an existing fact, use its supplied memory ID to update it instead of
                           adding a conflicting memory
                           """
        },
        AIContextProviders =
        [
            new Mem0AIContextProvider(mem0Client, userId: "user1", appId: "app1")
        ]
    }, model: "gpt-6-luna")
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