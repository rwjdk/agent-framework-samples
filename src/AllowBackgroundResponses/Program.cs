//YouTube video that cover this sample: https://youtu.be/6sM6nTk_UBs


using Microsoft.Agents.AI;
using Shared;
using System.ClientModel;
using OpenAI;
using OpenAI.Responses;
#pragma warning disable MEAI001

#pragma warning disable OPENAI001

Console.Clear();

Secrets secrets = SecretsManager.GetSecrets();

OpenAIClient client = ClientHelper.GetAzureOpenAIClient();

AIAgent agent = client
    .GetResponsesClient()
    .AsAIAgent(model: "gpt-6-luna");

Utils.Green("SimpleQuestion-BEGIN");
AgentResponse response1 = await agent.RunAsync("What is the capital of France?");
Console.WriteLine(response1);
Utils.Green("SimpleQuestion-END");

Console.Clear();

Utils.Green("BigQuestion-BEGIN");
AgentResponse response2 = await agent.RunAsync("Write a 1000 word essay on Pigs in Space");
Console.WriteLine(response2);
Utils.Green("BigQuestion-END");

Console.Clear();

Utils.Green("BigQuestion-BACKGROUND-BEGIN");
AgentSession agentSession = await agent.CreateSessionAsync();
ChatClientAgentRunOptions options = new()
{
    AllowBackgroundResponses = true
};
AgentResponse response3 = await agent.RunAsync("Write a 2000 word essay on Pigs in Space", agentSession, options: options);
Utils.Green("BigQuestion-BACKGROUND-END");

int counter = 0;
while (response3.ContinuationToken is not null)
{
    await Task.Delay(TimeSpan.FromSeconds(2));
    counter++;
    Utils.Gray($"- Waited: {(counter * 2)} seconds...");

    options.ContinuationToken = response3.ContinuationToken;
    response3 = await agent.RunAsync(agentSession, options);
}

Console.WriteLine(response3.Text);