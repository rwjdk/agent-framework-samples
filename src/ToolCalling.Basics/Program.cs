#pragma warning disable OPENAI001
using OpenAI.Responses;
//YouTube video that cover this sample
//- Basic: https://youtu.be/gJTodKpv8Ik
//- Advanced: https://youtu.be/dCtojrK8bKk
//- MCP: https://youtu.be/Y5IKdt9vdJM

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using Shared;
using System.ClientModel;

using OpenAI.Chat;
using ToolCalling.Basics;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

Secrets secrets = SecretsManager.GetSecrets();

OpenAIClient client = ClientHelper.GetAzureOpenAIClient();

ChatClientAgent agent = client
    .GetResponsesClient().AsIChatClient("gpt-6-luna")
    .AsAIAgent(
        instructions: "You are a Time Expert",
        tools:
        [
            AIFunctionFactory.Create(Tools.CurrentDataAndTime, "current_date_and_time"),
            AIFunctionFactory.Create(Tools.CurrentTimezone, "current_timezone")
        ]
    );

AgentSession session = await agent.CreateSessionAsync();

while (true)
{
    Console.Write("> ");
    string? input = Console.ReadLine();
    ChatMessage message = new(ChatRole.User, input);
    AgentResponse response = await agent.RunAsync(message, session);
    Console.WriteLine(response);

    Utils.Separator();
}