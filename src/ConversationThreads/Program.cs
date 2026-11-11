#pragma warning disable OPENAI001
using OpenAI.Responses;
//YouTube video that cover this sample: https://youtu.be/p5AvoMbgPtI
// ReSharper disable HeuristicUnreachableCode

#pragma warning disable CS0162 // Unreachable code detected

using Microsoft.Agents.AI;
using OpenAI;
using Shared;
using System.ClientModel;
using ConversationThreads;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

Secrets secrets = SecretsManager.GetSecrets();

OpenAIClient client = ClientHelper.GetAzureOpenAIClient();

ChatClientAgent agent = client
    .GetResponsesClient().AsIChatClient("gpt-6-luna")
    .AsAIAgent(new ChatClientAgentOptions
    {
        ChatOptions = new ChatOptions
        {
            Instructions = "You are a Friendly AI Bot, answering questions"
        },
        ChatHistoryProvider = new InMemoryChatHistoryProvider() //Note this is needed now in order to get the restored messages
    });

AgentSession session;

const bool optionToResume = true; //Set this to true to test resume of previous conversations

if (optionToResume)
{
    session = await AgentSessionPersistence.ResumeChatIfRequestedAsync(agent);
}
else
{
    session = await agent.CreateSessionAsync();
}

while (true)
{
    Console.Write("> ");
    string? input = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(input))
    {
        ChatMessage message = new(ChatRole.User, input);
        await foreach (AgentResponseUpdate update in agent.RunStreamingAsync(message, session))
        {
            Console.Write(update);
        }
    }

    Utils.Separator();

    if (optionToResume)
    {
        await AgentSessionPersistence.StoreThreadAsync(agent, session);
    }
}
