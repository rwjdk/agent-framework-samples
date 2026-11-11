//YouTube video that cover this sample: https://youtu.be/pqLWICXRtyA

#pragma warning disable OPENAI001

using Microsoft.Agents.AI;
using Shared;
using System.ClientModel;
using OpenAI;
using OpenAI.Responses;
using Shared.Extensions;

Secrets secrets = SecretsManager.GetSecrets();
Console.Clear();
//OpenAIClient client = new(secrets.OpenAiApiKey);
OpenAIClient client = ClientHelper.GetAzureOpenAIClient();

AIAgent agent = client
    .GetResponsesClient()
    .AsAIAgent(
        model: "gpt-6-luna",
        instructions: "You are a C# Developer"
    );

List<AgentResponseUpdate> updates = [];
string question = "Show me an C# Example of a method adding two numbers";
await foreach (AgentResponseUpdate update in agent.RunStreamingAsync(question))
{
    updates.Add(update);
    Console.Write(update);
}

AgentResponse fullResponse = updates.ToAgentResponse();
fullResponse.Usage.OutputAsInformation();