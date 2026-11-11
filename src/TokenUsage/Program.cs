#pragma warning disable OPENAI001
using OpenAI.Responses;
using Microsoft.Extensions.AI;
//YouTube video that cover this sample: https://youtu.be/ghND74Hj6Fs


using Microsoft.Agents.AI;
using OpenAI;
using Shared;
using System.ClientModel;
using OpenAI.Chat;
using Shared.Extensions;

Secrets secrets = SecretsManager.GetSecrets();

OpenAIClient client = ClientHelper.GetAzureOpenAIClient();

ChatClientAgent agent = client
    .GetResponsesClient().AsIChatClient("gpt-6-luna")
    .AsAIAgent(instructions: "You are a Friendly AI Bot, answering questions");

string question = "What is the capital of France and how many people live there?";

//Simple
AgentResponse response = await agent.RunAsync(question);
Console.WriteLine(response);

Utils.Gray($"- Input Tokens: {response.Usage?.InputTokenCount}");
Utils.Gray($"- Output Tokens: {response.Usage?.OutputTokenCount} " +
                        $"({response.Usage?.ReasoningTokenCount} was used for reasoning)");

//------------------------------------------------------------------------------------------------------------------------
Utils.Separator();

//Streaming
List<AgentResponseUpdate> updates = [];
await foreach (AgentResponseUpdate update in agent.RunStreamingAsync(question))
{
    updates.Add(update);
    Console.Write(update);
}

Console.WriteLine();

AgentResponse collectedResponseFromStreaming = updates.ToAgentResponse();
Utils.Gray($"- Input Tokens (Streaming): {collectedResponseFromStreaming.Usage?.InputTokenCount}");
Utils.Gray($"- Output Tokens (Streaming): {collectedResponseFromStreaming.Usage?.OutputTokenCount} " +
                        $"({collectedResponseFromStreaming.Usage?.ReasoningTokenCount} was used for reasoning)");

Utils.Separator();
Console.ReadKey();