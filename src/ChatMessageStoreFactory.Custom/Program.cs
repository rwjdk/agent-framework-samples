#pragma warning disable OPENAI001
using OpenAI.Responses;
using Microsoft.Extensions.AI;

using Microsoft.Agents.AI;
using OpenAI.Chat;
using Shared;
using System.Text.Json;
using OpenAI;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

Console.Clear();

OpenAIClient client = ClientHelper.GetAzureOpenAIClient();

ChatClientAgent agent = client
    .GetResponsesClient().AsIChatClient("gpt-6-luna")
    .AsAIAgent(
        new ChatClientAgentOptions
        {
            ChatHistoryProvider = new MyMessageStore()
        }
    );

AgentSession session = await agent.CreateSessionAsync();

AgentResponse response = await agent.RunAsync("Who is Barack Obama", session);
Console.WriteLine(response);

JsonElement sessionElement = await agent.SerializeSessionAsync(session);
string toStoreForTheUser = JsonSerializer.Serialize(sessionElement);

Utils.Separator();

//Some time passes....
Utils.Green("Some time passes, and we restore the session...");


JsonElement restoredSessionElement = JsonSerializer.Deserialize<JsonElement>(toStoreForTheUser);

AgentSession restoredThread = await agent.DeserializeSessionAsync(restoredSessionElement);

AgentResponse someTimeLaterResponse = await agent.RunAsync("How Tall is he?", restoredThread);
Console.WriteLine(someTimeLaterResponse);

class MyMessageStore() : ChatHistoryProvider
{
    public string? SessionId { get; set; }

    public string SessionPath => Path.Combine(Path.GetTempPath(), $"{SessionId}.json");

    private readonly List<ChatMessage> _messages = [];

    protected override async ValueTask<IEnumerable<ChatMessage>> ProvideChatHistoryAsync(InvokingContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        if(context.Session!.StateBag.TryGetValue("SessionId", out string? sessionId))
        {
            SessionId = sessionId!;
        }
        else
        {
            SessionId = Guid.NewGuid().ToString();
            context.Session.StateBag.SetValue("SessionId", SessionId);
        }
        if (!File.Exists(SessionPath))
        {
            return [];
        }

        string json = await File.ReadAllTextAsync(SessionPath, cancellationToken);
        return JsonSerializer.Deserialize<List<ChatMessage>>(json)!;
    }

    protected override async ValueTask StoreChatHistoryAsync(InvokedContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        // Add both request and response messages to the store
        // Optionally messages produced by the AIContextProvider can also be persisted (not shown).
        _messages.AddRange(context.RequestMessages.Concat(context.ResponseMessages ?? []));

        await File.WriteAllTextAsync(SessionPath, JsonSerializer.Serialize(_messages), cancellationToken);
    }
}