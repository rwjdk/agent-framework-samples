#pragma warning disable OPENAI001
//WARNING: This is a playground area for the creator of the Repo to test and tinker. Nothing in this project is as such educational and might not even execute properly
#pragma warning disable OPENAI002
using System.ClientModel;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Responses;
using Playground.Tools;
using Shared;
#pragma warning disable OPENAI001 //https://github.com/openai/openai-dotnet/issues/1130
#pragma warning disable MEAI001

Utils.Init("Playground");

Secrets secrets = SecretsManager.GetSecrets();


OpenAIClient azureOpenAIClient = ClientHelper.GetAzureOpenAIClient();

string model = "gpt-6-luna";
string question = "What is the Weather like in Paris?";
AIFunction tool = AIFunctionFactory.Create(GetWeather, "get_weather_for_city");

AIAgent chatClientAgent = azureOpenAIClient.GetResponsesClient().AsIChatClient(model).AsAIAgent(new ChatClientAgentOptions
{
    ChatOptions = new ChatOptions
    {
        Tools = [tool],
        RawRepresentationFactory = chatClient => new CreateResponseOptions
        {
            ReasoningOptions = new ResponseReasoningOptions { ReasoningEffortLevel = ResponseReasoningEffortLevel.Low },
            }
    }
});
AgentResponse chatClientAgentResponse = await chatClientAgent.RunAsync(question);

AIAgent responsesApiAgent = azureOpenAIClient.GetResponsesClient().AsAIAgent(new ChatClientAgentOptions
{
    ChatOptions = new ChatOptions
    {
        Tools = [tool],
        RawRepresentationFactory = chatClient => new CreateResponseOptions
        {
            ReasoningOptions = new ResponseReasoningOptions
            {
                ReasoningEffortLevel = ResponseReasoningEffortLevel.Low,
            }
        }
    }
}, model);

AgentResponse responsesApiAgentResponse = await responsesApiAgent.RunAsync(question);





AgentBuilderTool agentBuilderTool = new AgentBuilderTool();

OpenAIClient client = ClientHelper.GetAzureOpenAIClient();

AIAgent agent = client
    .GetResponsesClient()
    .AsAIAgent(
        model: "gpt-6-luna"
        //tools: [AIFunctionFactory.Create(agentBuilderTool.RunSubAgent, "call_sub_agent")]
        );

AgentSession session = await agent.CreateSessionAsync();

while (true)
{
    Console.Write("> ");
    string? input = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(input))
    {
        ChatClientAgentRunOptions options = new()
        {
            AllowBackgroundResponses = true,
            ChatOptions = new ChatOptions
            {
            }
        };
        AgentResponse response = await agent.RunAsync(input, session, options: options);
        int counter = 0;
        while (response.ContinuationToken is not null)
        {
            await Task.Delay(TimeSpan.FromSeconds(2));
            counter++;
            Utils.Gray($"- Waited: {(counter * 2)} seconds...");
            options.ContinuationToken = response.ContinuationToken;
            response = await agent.RunAsync(session, options);
        }
        Console.WriteLine(response);
    }

    Utils.Separator();

}

string GetWeather(string city)
{
    return "It is sunny and 19 degrees";
}