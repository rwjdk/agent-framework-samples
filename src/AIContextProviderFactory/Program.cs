#pragma warning disable OPENAI001
using OpenAI.Responses;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Shared;
using System.ClientModel;
using JetBrains.Annotations;
using OpenAI;
using OpenAI.Chat;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

Console.Clear();
Secrets secrets = SecretsManager.GetSecrets();

string userId = "rwj1234";

OpenAIClient client = ClientHelper.GetAzureOpenAIClient();

ChatClientAgent memoryExtractorAgent = client
    .GetResponsesClient().AsIChatClient("gpt-6-luna")
    .AsAIAgent(
        instructions: "Look at the user's message and extract any memory that we do not already know (or non if there aren't any memories to store)"
    );

ChatClientAgent agentWithCustomMemory = client.GetResponsesClient().AsIChatClient("gpt-6-luna")
    .AsAIAgent(new ChatClientAgentOptions
    {
        ChatOptions = new()
        {
            Instructions = "You are a nice AI"
        },
        AIContextProviders = [new CustomContextProvider(memoryExtractorAgent, userId)]
    });

AIAgent agentToUse = agentWithCustomMemory;

AgentSession session = await agentToUse.CreateSessionAsync();

while (true)
{
    Console.Write("> ");
    string? input = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(input))
    {
        ChatMessage message = new(ChatRole.User, input);
        AgentResponse response = await agentToUse.RunAsync(message, session);
        {
            Console.WriteLine(response);
        }
    }

    Utils.Separator();
}

class CustomContextProvider : AIContextProvider
{
    private readonly ChatClientAgent _memoryExtractorAgent;
    private readonly List<string> _userFacts = [];
    private readonly string _userMemoryFilePath;

    public CustomContextProvider(ChatClientAgent memoryExtractorAgent, string userId)
    {
        _memoryExtractorAgent = memoryExtractorAgent;
        _userMemoryFilePath = Path.Combine(Path.GetTempPath(), $"{userId}.txt");
        if (File.Exists(_userMemoryFilePath))
        {
            _userFacts.AddRange(File.ReadAllLines(_userMemoryFilePath));
        }
    }

    protected override ValueTask<AIContext> ProvideAIContextAsync(InvokingContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        return ValueTask.FromResult(new AIContext
        {
            Instructions = $" - User facts: {string.Join(" | ", _userFacts)}"
        });
    }

    protected override async ValueTask StoreAIContextAsync(InvokedContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        ChatMessage lastMessageFromUser = context.RequestMessages.Last();
        List<ChatMessage> inputToMemoryExtractor =
        [
            new(ChatRole.Assistant, $"We know the following about the user already and should not extract that again: {string.Join(" | ", _userFacts)}"),
            lastMessageFromUser
        ];

        AgentResponse<MemoryUpdate> response = await _memoryExtractorAgent.RunAsync<MemoryUpdate>(inputToMemoryExtractor, cancellationToken: cancellationToken);
        foreach (string memoryToRemove in response.Result.MemoryToRemove)
        {
            _userFacts.Remove(memoryToRemove);
        }

        _userFacts.AddRange(response.Result.MemoryToAdd);
        await File.WriteAllLinesAsync(_userMemoryFilePath, _userFacts.Distinct(), cancellationToken);
    }

    [UsedImplicitly]
    private record MemoryUpdate(List<string> MemoryToAdd, List<string> MemoryToRemove);
}