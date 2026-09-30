using System.Diagnostics;
using AgentFrameworkToolkit.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Responses;
using Shared;
#pragma warning disable OPENAI001

Utils.Init("OpenAI - Ultrafast mode");

Secrets secrets = SecretsManager.GetSecrets();

OpenAIConnection openAIConnection = new()
{
    ApiKey = secrets.OpenAiApiKey,
    DefaultClientType = ClientType.ResponsesApi
};

//Agent Framework Toolkit
OpenAIAgent openAIAgent = new OpenAIAgentFactory(openAIConnection).CreateAgent(new AgentOptions
{
    Model = "gpt-6-astra",
    ServiceTier = OpenAIServiceTier.Ultrafast
});

Stopwatch stopwatch = Stopwatch.StartNew();
string q1 = "What is the capital of France?";
Utils.Green(q1);
AgentResponse agentResponse1 = await openAIAgent.RunAsync(q1);
Console.WriteLine(agentResponse1);
Utils.Gray($"Took: {Math.Round(stopwatch.Elapsed.TotalSeconds, 2)} Seconds");

Utils.Separator();

string q2 = "Write a 1000 word essay on how cool Ducks are";
Utils.Green(q2);
AgentResponse agentResponse2 = await openAIAgent.RunAsync(q2);
Console.WriteLine(agentResponse2.Text.Substring(0, 100)+"...");
Utils.Gray($"Took: {Math.Round(stopwatch.Elapsed.TotalSeconds, 2)} Seconds");

#region Manual setup without Agent Framework Toolkit
OpenAIClient client = openAIConnection.GetClient();
ChatClientAgent agent = client.GetResponsesClient().AsAIAgent(
    new ChatClientAgentOptions
    {
        ChatOptions = new ChatOptions
        {
            RawRepresentationFactory = chatClient => new CreateResponseOptions
            {
                ServiceTier = new ResponseServiceTier("ultrafast")
            }
        }
    }
    , model: "gpt-6-astra");

#endregion