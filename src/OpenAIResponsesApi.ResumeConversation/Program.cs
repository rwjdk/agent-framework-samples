//YouTube video that cover this sample: https://youtu.be/sfXaW4fZrh0

using System.ClientModel;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Responses;
using Shared;

#pragma warning disable OPENAI001
Console.Clear();
Secrets secrets = SecretsManager.GetSecrets();

OpenAIClient client = new(secrets.OpenAiApiKey);
/*OpenAIClient client = ClientHelper.GetAzureOpenAIClient();*/
ResponsesClient responseClient = client
    .GetResponsesClient();

AIAgent agent = responseClient
    .AsAIAgent(
        model: "gpt-6-luna",
        instructions: "You are a Nice AI"
    );

AgentSession session = await agent.CreateSessionAsync();

AgentResponse response1 = await agent.RunAsync("Who is Barak Obama? (Max 5 words)", session);
Console.WriteLine(response1);

AgentResponse response2 = await agent.RunAsync("How Tall is he?", session);
Console.WriteLine(response2);

//Imagine some time go by and user come back and the in-process thread is gone and not stored... Only the conversation ID
string? responseId = response2.ResponseId;

//Get previous text calling this multiple times
ClientResult<ResponseResult>? result = await responseClient.GetResponseAsync(responseId);

AgentResponse response3 = await agent.RunAsync("What city is he from", options: new ChatClientAgentRunOptions
{
    ChatOptions = new ChatOptions
    {
        ConversationId = responseId
    }
});
Console.WriteLine(response3);