#pragma warning disable OPENAI001
using OpenAI.Responses;
using System.Text;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using Shared;
using OpenAI.Chat;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;
#pragma warning disable MEAI001

Utils.Init("Custom ChatHistory Reducers");
OpenAIClient client = ClientHelper.GetAzureOpenAIClient();

//Built-in reducers (covered in previous video)
IChatReducer messageCountingChatReducer = new MessageCountingChatReducer(5);
IChatReducer summarizingChatReducer = new SummarizingChatReducer(client.GetResponsesClient().AsIChatClient("gpt-6-luna"), targetCount: 1, threshold: 4);

//Custom reducers
IChatReducer alwaysRemoveToolCallsReducer = new AlwaysRemoveToolCallsReducer();

IChatReducer messagesWithWordReducer = new MessagesWithWordReducer("Sunny");

ChatClientAgent pirateSummaryReducerAgent = client
    .GetResponsesClient().AsIChatClient("gpt-6-luna")
    .AsAIAgent(instructions: "Given the input messages make a summary of them in the voice of a pirate!");
AIDrivenPirateSummaryReducer aiDrivenSummaryReducer = new AIDrivenPirateSummaryReducer(pirateSummaryReducerAgent, 4);


ChatClientAgent cityReducerAgent = client
    .GetResponsesClient().AsIChatClient("gpt-6-luna")
    .AsAIAgent(instructions: "Given the input numbered messages, return the numbers of the messages that contain a city name");
AIDrivenCityReducer aiDrivenCityReducer = new AIDrivenCityReducer(cityReducerAgent);

ChatClientAgent agent = client.GetResponsesClient().AsIChatClient("gpt-6-luna")
    .AsAIAgent(new ChatClientAgentOptions
    {
        ChatOptions = new ChatOptions
        {
            Instructions = "You are a nice AI",
            Tools = [AIFunctionFactory.Create(GetWeather, "get_weather")]
        },
        ChatHistoryProvider = new InMemoryChatHistoryProvider(new InMemoryChatHistoryProviderOptions
        {
            ChatReducer = aiDrivenCityReducer
        })
    });

AgentSession session = await agent.CreateSessionAsync();

while (true)
{
    Console.Write("> ");
    string input = Console.ReadLine() ?? string.Empty;
    AgentResponse response = await agent.RunAsync(input, session);
    Console.WriteLine(response);
    InMemoryChatHistoryProvider? provider = agent.GetService<InMemoryChatHistoryProvider>();
    List<ChatMessage> messagesInSession = provider?.GetMessages(session) ?? [];
    Utils.Gray("- Number of messages in session: " + messagesInSession.Count());
    foreach (ChatMessage message in messagesInSession)
    {
        if (!string.IsNullOrWhiteSpace(message.Text))
        {
            Utils.Gray($"-- [{message.Role}] {message.Text}");
        }
        else
        {
            foreach (AIContent content in message.Contents)
            {
                switch (content)
                {
                    case FunctionCallContent functionCallContent:
                        Utils.Gray($"-- [{message.Role}] Tool Call {functionCallContent.Name} [Args: {string.Join(",", functionCallContent.Arguments?.Select(x => $"{x.Key}={x.Value}") ?? [])}]");
                        break;
                    case FunctionResultContent functionResultContent:
                        Utils.Gray($"-- [{message.Role}] Tool Result: {functionResultContent.Result}");
                        break;
                }
            }
        }
    }
    Utils.Separator();
}

static string GetWeather(string city)
{
    return "Sunny and 19 degrees";
}


class AlwaysRemoveToolCallsReducer : IChatReducer
{
    public Task<IEnumerable<ChatMessage>> ReduceAsync(IEnumerable<ChatMessage> previousMessages, CancellationToken cancellationToken)
    {
        Utils.Yellow("AlwaysRemoveToolCallsReducer called");
        List<ChatMessage> toKeep = [];
        foreach (ChatMessage message in previousMessages)
        {
            if (message.Role == ChatRole.Tool)
            {
                continue; //Get rid of Tool Results
            }

            if (message.Role == ChatRole.Assistant && message.Contents.Any(x => x is FunctionCallContent))
            {
                continue; //Get rid of the Tool Requests from the AI
            }

            toKeep.Add(message);
        }

        return Task.FromResult(toKeep.AsEnumerable());
    }
}

class MessagesWithWordReducer(string word) : IChatReducer
{
    public Task<IEnumerable<ChatMessage>> ReduceAsync(IEnumerable<ChatMessage> previousMessages, CancellationToken cancellationToken)
    {
        Utils.Yellow("MessagesWithWordReducer called");
        return Task.FromResult(previousMessages.Where(x => !x.Text.Contains(word, StringComparison.InvariantCultureIgnoreCase)));
    }
}

class AIDrivenPirateSummaryReducer(AIAgent agent, int numberOfPreviousMessagesBeforeSummarize) : IChatReducer
{
    public async Task<IEnumerable<ChatMessage>> ReduceAsync(IEnumerable<ChatMessage> previousMessages, CancellationToken cancellationToken)
    {
        Utils.Yellow("AIDrivenPirateSummaryReducer called");
        List<ChatMessage> messages = previousMessages.ToList();
        if (messages.Count <= numberOfPreviousMessagesBeforeSummarize)
        {
            Utils.Yellow("[No summary yet]");
            return messages;
        }

        Utils.Yellow("[Summarizing...]");
        AgentResponse response = await agent.RunAsync(messages, cancellationToken: cancellationToken);

        return new List<ChatMessage>
        {
            new(ChatRole.User, "Summary so far: "+response.Text)
        };
    }
}

class AIDrivenCityReducer(AIAgent agent) : IChatReducer
{
    public async Task<IEnumerable<ChatMessage>> ReduceAsync(IEnumerable<ChatMessage> previousMessages, CancellationToken cancellationToken)
    {
        Utils.Yellow("AIDrivenCityReducer called");
        List<ChatMessage> messages = previousMessages.ToList();
        if (messages.Count <= 0)
        {
            return messages;
        }

        AlwaysRemoveToolCallsReducer alwaysRemoveToolCallsReducer = new();
        List<ChatMessage> normalMessages = (await alwaysRemoveToolCallsReducer.ReduceAsync(messages, cancellationToken)).ToList();

        StringBuilder messagesAsString = new();
        for (int i = 0; i < normalMessages.Count; i++)
        {
            messagesAsString.AppendLine($"{i}: {normalMessages[i].Text}");
        }

        AgentResponse<List<int>> response = await agent.RunAsync<List<int>>(messagesAsString.ToString(), cancellationToken: cancellationToken);
        List<int> indexesToExclude = response.Result;

        List<ChatMessage> toKeep = [];
        toKeep.AddRange(normalMessages.Where((_, i) => !indexesToExclude.Contains(i)));

        return toKeep;
    }
}