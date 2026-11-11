#pragma warning disable OPENAI001
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Responses;
using Shared;
using Shared.Extensions;

Console.Clear();
Secrets secrets = SecretsManager.GetSecrets();
OpenAIClient client = new(secrets.OpenAiApiKey);
//NB: Azure OpenAI is NOT SUPPORTED
#pragma warning disable OPENAI001
AIAgent agent = client
    //.GetResponsesClient().AsIChatClient("gpt-6-luna")
    .GetResponsesClient()
#pragma warning restore OPENAI001
    .AsAIAgent(
        model: "gpt-6-luna",
        instructions: "You are a Space News AI Reporter",
        tools: [new HostedWebSearchTool()]
    );

List<AgentResponseUpdate> updates = [];
string question = "What is today's news in Space Exploration (List today's date at the top)";
await foreach (AgentResponseUpdate update in agent.RunStreamingAsync(question))
{
    updates.Add(update);
    Console.Write(update);
}

AgentResponse fullResponse = updates.ToAgentResponse();
fullResponse.Usage.OutputAsInformation();