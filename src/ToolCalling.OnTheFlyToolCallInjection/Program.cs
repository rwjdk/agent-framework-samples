#pragma warning disable OPENAI001
using OpenAI.Responses;
using AgentFrameworkToolkit.Tools.Common;
using JetBrains.Annotations;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using Shared;
using Shared.Extensions;
using System.ClientModel;
using System.Text;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

Console.Clear();

Secrets secrets = SecretsManager.GetSecrets();

Console.Write("Normal (N) or Inject Mode (I)?: ");
ConsoleKeyInfo key = Console.ReadKey();
Console.Clear();
switch (key.Key)
{
    case ConsoleKey.N:
        await NormalAgentWithTools();
        break;
    case ConsoleKey.I:
        await ToolInjection();
        break;
    default:
        Console.WriteLine("Invalid choice");
        break;
}

async Task NormalAgentWithTools()
{
    List<AITool> tools = [];
    tools.AddRange(TimeTools.All());
    tools.AddRange(FileSystemTools.All(new FileSystemToolsOptions
    {
        ConfinedToTheseFolderPaths = ["C:\\TestAI"]
    }));
    tools.AddRange(WeatherTools.All(new OpenWeatherMapOptions
    {
        ApiKey = secrets.OpenWeatherApiKey,
        PreferredUnits = WeatherOptionsUnits.Metric
    }));

    OpenAIClient client = ClientHelper.GetAzureOpenAIClient();
    AIAgent mainAgent = client.GetResponsesClient().AsIChatClient("gpt-6-luna").AsAIAgent(tools: tools).AsBuilder().Use(FunctionCallMiddleware).Build();

    Utils.Gray($"This agent have: {tools.Count} tools");
    foreach (AITool tool in tools)
    {
        Utils.Gray($"- {tool.Name}");
    }
    while (true)
    {
        Console.Write("> ");
        string input = Console.ReadLine() ?? "";
        AgentResponse response = await mainAgent.RunAsync(input);
        Console.WriteLine(response);
        response.Usage.OutputAsInformation();
        Utils.Separator();
    }
    // ReSharper disable once FunctionNeverReturns
}

async Task ToolInjection()
{
    OpenAIClient client = ClientHelper.GetAzureOpenAIClient();

    ChatClientAgent toolInjectionAgent = client.GetResponsesClient().AsIChatClient("gpt-6-luna").AsAIAgent(
        instructions: "You job is to tell if any given message is a request to use specific tools"
    );

    AIAgent mainAgent = client.GetResponsesClient().AsIChatClient("gpt-6-luna").AsAIAgent(new ChatClientAgentOptions
    {
        AIContextProviders = [new OnTheFlyToolInjectionContext(toolInjectionAgent, secrets)]
    }).AsBuilder().Use(FunctionCallMiddleware).Build();

    Utils.Gray("This agent have: 0 tools");
    while (true)
    {
        Console.Write("> ");
        string input = Console.ReadLine() ?? "";
        AgentResponse response = await mainAgent.RunAsync(input);
        Console.WriteLine(response);
        response.Usage.OutputAsInformation();
        Utils.Separator();
    }
    // ReSharper disable once FunctionNeverReturns
}

static async ValueTask<object?> FunctionCallMiddleware(AIAgent callingAgent, FunctionInvocationContext context, Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next, CancellationToken cancellationToken)
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

class OnTheFlyToolInjectionContext(ChatClientAgent toolInjectionAgent, Secrets secrets) : AIContextProvider
{
    protected override async ValueTask<AIContext> ProvideAIContextAsync(InvokingContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        IEnumerable<ChatMessage> messages = context.AIContext.Messages ?? [];
        AgentResponse<ToolResult> response = await toolInjectionAgent.RunAsync<ToolResult>(messages, cancellationToken: cancellationToken);
        List<AITool> injectedTools = [];
        string? injectedInstructions = null;
        ToolResult toolResult = response.Result;

        if (toolResult.NeedTimeTools)
        {
            Utils.Green("Time tools injected");
            injectedTools.AddRange(TimeTools.All());
        }

        if (toolResult.NeedFileSystemTools)
        {
            Utils.Green("File System Tools injected");
            injectedTools.AddRange(FileSystemTools.All(new FileSystemToolsOptions
            {
                ConfinedToTheseFolderPaths = ["C:\\TestAI"]
            }));
            injectedInstructions = "When working with files your root folder is 'C:\\TestAI'";
        }

        if (toolResult.NeedWeatherTools)
        {
            Utils.Green("Weather Tools injected");
            injectedTools.AddRange(WeatherTools.All(new OpenWeatherMapOptions
            {
                ApiKey = secrets.OpenWeatherApiKey,
                PreferredUnits = WeatherOptionsUnits.Metric
            }));
        }

        Utils.Green($"Number of tool's injected: {injectedTools.Count}");

        return new AIContext
        {
            Instructions = injectedInstructions,
            Tools = injectedTools
        };
    }

    [PublicAPI]
    private class ToolResult
    {
        public bool NeedFileSystemTools { get; set; }
        public bool NeedTimeTools { get; set; }
        public bool NeedWeatherTools { get; set; }
    }
}



