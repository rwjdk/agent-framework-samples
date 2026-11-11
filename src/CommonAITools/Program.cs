using AgentFrameworkToolkit;
using AgentFrameworkToolkit.AzureOpenAI;
using AgentFrameworkToolkit.OpenAI;using AgentFrameworkToolkit.Tools;
using AgentFrameworkToolkit.Tools.Common;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Shared;
using Shared.Extensions;

Secrets secrets = SecretsManager.GetSecrets();

Console.Clear();

List<AITool> tools = [];

//Time Tools
tools.AddRange(TimeTools.All());

//Weather Tools
tools.AddRange(WeatherTools.All(new OpenWeatherMapOptions
{
    ApiKey = secrets.OpenWeatherApiKey,
    PreferredUnits = WeatherOptionsUnits.Metric,
}));

//File System Tools
tools.AddRange(FileSystemTools.All(new FileSystemToolsOptions
{
    ConfinedToTheseFolderPaths = ["C:\\TestAI"]
}));

//Website Tools
tools.AddRange(WebsiteTools.All());

//HTTP Client Tools
tools.AddRange(HttpClientTools.All());

#region Alternative way for even more control

AIToolsFactory toolsFactory = new AIToolsFactory();
tools.AddRange(toolsFactory.GetTimeTools(new GetTimeToolsOptions
{
    GetUtcNow = true,
    GetUtcNowToolName = "GetUTC",
    GetUtcNowToolDescription = "Get Time in UTC format",

    GetLocalNow = true,
    GetLocalNowToolName = "GetLocalTime",
    GetLocalNowToolDescription = "Get the Local Time",
    GetNowLocalOptions = new GetNowLocalOptions
    {
        IncludeTimezoneParameter = false,
        DefaultLocalTimezoneIdIfNoneIsProvided = "Europe/Copenhagen"
    }
}));
#endregion

//Agent Part
AzureOpenAIAgentFactory factory = new AzureOpenAIAgentFactory(secrets.AzureOpenAiEndpoint, secrets.AzureOpenAiKey);
factory.Connection.DefaultClientType = global::AgentFrameworkToolkit.OpenAI.ClientType.ResponsesApi;
AIAgent agent = factory.CreateAgent(new AgentOptions
{
    Instructions = $"""
                   You are an Agent with many tools
                   - When using Trello use this API Key: '{secrets.TrelloApiKey}' and Token '{secrets.TrelloToken}'
                   - When working with Files the operating folder it C:\TestAI"
                   - When working with time Report it back in Denmark Time
                   - When working with Weather report back in metric units
                   """,
    Model = OpenAIChatModels.Gpt6Luna,
    //Tools = tools,
    RawToolCallDetails = Console.WriteLine
});

AgentSession session = await agent.CreateSessionAsync();
while (true)
{
    Console.Write("> ");
    string input = Console.ReadLine() ?? "";
    if (input == "/new")
    {
        Console.Clear();
        session = await agent.CreateSessionAsync();
        continue;
    }
    AgentResponse response = await agent.RunAsync(input, session);
    Console.WriteLine(response);
    response.Usage.OutputAsInformation();
    Utils.Separator();
}