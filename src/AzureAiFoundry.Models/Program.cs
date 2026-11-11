#pragma warning disable OPENAI001
using OpenAI.Responses;
using Microsoft.Extensions.AI;
//YouTube video that cover this sample: https://youtu.be/arfBt6-wbnw

using Azure;
using Azure.AI.Projects;

using Azure.Identity;
using Microsoft.Agents.AI;
using OpenAI;
using Shared;
using System.ClientModel;
using OpenAI.Chat;

Secrets secrets = SecretsManager.GetSecrets();

string questionToAsk = "What is the capital of the USA?";

await CreateAndCallNormalClientAgent("gpt-6-luna", questionToAsk); //Works
await CreateAndCallNormalClientAgent("DeepSeek-R1-0528", questionToAsk); //Works

await CreateAndCallFoundryAgent("gpt-6-luna", questionToAsk); //Works
// The project Responses example uses the deployed OpenAI chat model.

async Task CreateAndCallNormalClientAgent(string model, string question)
{
    OpenAIClient client = ClientHelper.GetAzureOpenAIClient();

    IChatClient chatClient = model.StartsWith("gpt-", StringComparison.OrdinalIgnoreCase)
        ? client.GetResponsesClient().AsIChatClient(model)
        : client.GetChatClient(model).AsIChatClient();
    ChatClientAgent agent = chatClient.AsAIAgent(instructions: "You are a Friendly AI Bot, answering questions");

    AgentResponse response = await agent.RunAsync(question);
    Utils.Yellow($"Answer from IChatClient using Model: '{model}'");
    Console.WriteLine($"Answer ='{response}'");
    Utils.Separator();
}

async Task CreateAndCallFoundryAgent(string model, string question)
{
    AIProjectClient projectClient = new(new Uri(secrets.MicrosoftFoundryEndpoint), new AzureCliCredential());
    ChatClientAgent agent = projectClient.ProjectOpenAIClient.GetResponsesClient().AsAIAgent(
        model: model, instructions: "You are a Friendly AI Bot, answering questions");
    Console.WriteLine(await agent.RunAsync(question));
}
