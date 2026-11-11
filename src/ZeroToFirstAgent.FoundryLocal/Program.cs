#pragma warning disable OPENAI001
using OpenAI.Responses;
using Microsoft.Extensions.AI;
using System.ClientModel;
using Microsoft.Agents.AI;
using Microsoft.AI.Foundry.Local;
using Microsoft.Extensions.Logging.Abstractions;
using OpenAI;
using OpenAI.Chat;
using Shared;

Utils.Init("Foundry.Local Sample");

const string modelAlias = "qwen3-0.6b";
const DeviceType deviceType = DeviceType.CPU;

Utils.Green($"Starting AI Model '{modelAlias}'. If not already started / cached this might take a while...");
string foundryDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".foundry");

Utils.Gray($"- Using Foundry data directory: {foundryDataDir} (Try removing this folder if code hangs, or cause Exceptions");
Utils.Gray("- Initializing Foundry Local SDK...");
await FoundryLocalManager.CreateAsync(
    new Configuration
    {
        AppName = "ZeroToFirstAgentFoundryLocal",
        AppDataDir = foundryDataDir,
        Web = new Configuration.WebService
        {
            Urls = "http://127.0.0.1:0"
        }
    },
    NullLogger.Instance);

FoundryLocalManager foundryLocal = FoundryLocalManager.Instance;

Utils.Gray("- Getting Catalog and Models...");
ICatalog catalog = await foundryLocal.GetCatalogAsync();
List<IModel> models = await catalog.ListModelsAsync();
Utils.Gray("- Available Models");
foreach (IModel availableModels in models)
{
    Utils.Gray($"-- {availableModels.Alias}");
}

IModel modelFamily = await catalog.GetModelAsync(modelAlias) ?? throw new InvalidOperationException($"Model '{modelAlias}' not found.");
IModel model = modelFamily.Variants.First(v => v.Info.Runtime?.DeviceType == deviceType);
Utils.Gray($"- Selected model variant: {model.Id}");

if (!await model.IsCachedAsync())
{
    Utils.Gray("- Model not yet cached yet. Downloading...");
    await model.DownloadAsync();
}

Utils.Gray("- Loading model into memory...");
await model.LoadAsync();

Utils.Gray("Starting local OpenAI-compatible web service...");
await foundryLocal.StartWebServiceAsync();

Utils.Separator();

Utils.Green("Creating OpenAI Compatible Client and Agent");
try
{
    string serviceUrl = foundryLocal.Urls!.First();
    OpenAIClient client = new(new ApiKeyCredential("NO_API_KEY"), new OpenAIClientOptions
    {
        Endpoint = new Uri($"{serviceUrl}/v1")
    });
    ChatClientAgent agent = client.GetResponsesClient().AsIChatClient(model.Id).AsAIAgent();

    //Normal
    const string question1 = "What is the Capital of Sweden?";
    Utils.Yellow($"Q: {question1}");
    AgentResponse response = await agent.RunAsync(question1);
    Console.WriteLine(response);

    Utils.Separator();

    //Streaming
    const string question2 = "How to make tomato soup?";
    Utils.Yellow($"Q: {question2}");
    await foreach (AgentResponseUpdate update in agent.RunStreamingAsync(question2))
    {
        Console.Write(update);
    }
}
finally
{
    await foundryLocal.StopWebServiceAsync();
    await model.UnloadAsync();
}

Utils.Green("-- Done --");
