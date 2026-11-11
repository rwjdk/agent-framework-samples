#pragma warning disable OPENAI001
using AgentFrameworkToolkit.AzureOpenAI;
using AgentFrameworkToolkit.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Responses;
using Shared;
using System.ClientModel;
#pragma warning disable OPENAI001 //https://github.com/openai/openai-dotnet/issues/1130

Utils.Init("It is Time to Switch to OpenAI Responses API");

Secrets secrets = SecretsManager.GetSecrets();

OpenAIClient client = ClientHelper.GetAzureOpenAIClient();

AzureOpenAIAgentFactory azureOpenAIAgentFactory = new(new AzureOpenAIConnection
{
    DefaultClientType = ClientType.ResponsesApi,
    Endpoint = secrets.AzureOpenAiEndpoint,
    ApiKey = secrets.AzureOpenAiKey
});

string question = "What is the Weather like in Paris?";
AIFunction tool = AIFunctionFactory.Create(GetWeather, "get_weather_for_city");

//string modelToUse = "gpt-6-luna";
const string modelToUse = "gpt-6-luna";

await ResponsesClientWithTool(modelToUse);

await ResponsesClientWithLowReasoning(modelToUse);

await ResponsesClientWithToolAndLowReasoning(modelToUse);

await ResponsesApiWithToolAndLowReasoning(modelToUse);

await AgentFrameworkToolkitWithToolAndLowReasoning(modelToUse);

return;

static string GetWeather(string city)
{
    return "It is sunny and 19 degrees";
}

async Task ResponsesClientWithTool(string model)
{
    try
    {
        Utils.Yellow($"IResponses client with Tool answering Question: '{question}' with model: {model}");
        AIAgent agent = client.GetResponsesClient().AsIChatClient(model).AsAIAgent(tools: [tool]);
        AgentResponse response = await agent.RunAsync(question);
        Console.WriteLine(response);
    }
    catch (Exception e)
    {
        Utils.Red($"Error: {e.Message}");
    }
    finally
    {
        Utils.Gray("---");
    }
}

async Task ResponsesClientWithLowReasoning(string model)
{
    try
    {
        Utils.Yellow($"IResponses client with Reasoning answering Question: '{question}' with model: {model}");
        AIAgent agent = client.GetResponsesClient().AsIChatClient(model).AsAIAgent(new ChatClientAgentOptions
        {
            ChatOptions = new ChatOptions
            {
                RawRepresentationFactory = _ => new CreateResponseOptions
                {
                    ReasoningOptions = new ResponseReasoningOptions { ReasoningEffortLevel = ResponseReasoningEffortLevel.Low },
                    }
            }
        });
        AgentResponse response = await agent.RunAsync(question);
        Console.WriteLine(response);
    }
    catch (Exception e)
    {
        Utils.Red($"Error: {e.Message}");
    }
    finally
    {
        Utils.Gray("---");
    }
}

async Task ResponsesClientWithToolAndLowReasoning(string model)
{
    try
    {
        Utils.Yellow($"IResponses client with Tool and Reasoning answering Question: '{question}' with model: {model}");
        AIAgent agent = client.GetResponsesClient().AsIChatClient(model).AsAIAgent(new ChatClientAgentOptions
        {
            ChatOptions = new ChatOptions
            {
                Tools = [tool],
                RawRepresentationFactory = _ => new CreateResponseOptions
                {
                    ReasoningOptions = new ResponseReasoningOptions { ReasoningEffortLevel = ResponseReasoningEffortLevel.Low },
                    }
            }
        });
        AgentResponse response = await agent.RunAsync(question);
        Console.WriteLine(response);
    }
    catch (Exception e)
    {
        Utils.Red($"Error: {e.Message}");
    }
    finally
    {
        Utils.Gray("---");
    }
}

async Task ResponsesApiWithToolAndLowReasoning(string model)
{
    try
    {
        Utils.Yellow($"ResponsesAPI with Tool and Reasoning answering Question: '{question}' with model: {model}");
        AIAgent agent = client.GetResponsesClient().AsAIAgent(new ChatClientAgentOptions
        {
            ChatOptions = new ChatOptions
            {
                Tools = [tool],
                RawRepresentationFactory = _ => new CreateResponseOptions
                {
                    ReasoningOptions = new ResponseReasoningOptions
                    {
                        ReasoningEffortLevel = ResponseReasoningEffortLevel.Low,
                    }
                }
            }
        }, model);

        AgentResponse response = await agent.RunAsync(question);
        Console.WriteLine(response);
    }
    catch (Exception e)
    {
        Utils.Red($"Error: {e.Message}");
    }
    finally
    {
        Utils.Gray("---");
    }
}

async Task AgentFrameworkToolkitWithToolAndLowReasoning(string model)
{
    try
    {
        Utils.Yellow($"AgentFrameworkToolkit with Tool and Reasoning answering Question: '{question}' with model: {model}");
        AzureOpenAIAgent agent = azureOpenAIAgentFactory.CreateAgent(new AgentOptions
        {
            ClientType = ClientType.ResponsesApi,
            Model = model,
            Tools = [tool],
            ReasoningEffort = OpenAIReasoningEffort.Low
        });
        AgentResponse response = await agent.RunAsync(question);
        Console.WriteLine(response);
    }
    catch (Exception e)
    {
        Utils.Red($"Error: {e.Message}");
    }
    finally
    {
        Utils.Gray("---");
    }
}