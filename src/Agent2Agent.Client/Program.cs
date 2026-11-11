#pragma warning disable OPENAI001
using OpenAI.Responses;
using Microsoft.Extensions.AI;
//YouTube video that cover this sample: https://youtu.be/g72ks3rY9qQ

using A2A;

using Microsoft.Agents.AI;
using Shared;
using System.ClientModel;
using OpenAI;
using OpenAI.Chat;

Utils.Gray("Initializing");
Utils.Gray("- Waiting 1 sec for the server to be ready");
await Task.Delay(1000);
Secrets secrets = SecretsManager.GetSecrets();

Utils.Gray("- Connecting to Remote Agent");
A2ACardResolver agentCardResolver = new A2ACardResolver(new Uri("http://localhost:5000"));
AIAgent remoteAgent = await agentCardResolver.GetAIAgentAsync();

Utils.Separator();

Utils.Gray("Ready for questions");

OpenAIClient client = ClientHelper.GetAzureOpenAIClient();

ChatClientAgent agent = client
    .GetResponsesClient().AsIChatClient("gpt-6-luna")
    .AsAIAgent(
        name: "ClientAgent",
        instructions: "You specialize in handling queries for users and using your tools to provide answers.",
        tools: [remoteAgent.AsAIFunction()]);

AgentSession session = await agent.CreateSessionAsync();
while (true)
{
    Console.Write("> ");
    string message = Console.ReadLine() ?? string.Empty;
    if (message == string.Empty)
    {
        continue;
    }

    AgentResponse response = await agent.RunAsync(message, session);
    Console.WriteLine(response);
}