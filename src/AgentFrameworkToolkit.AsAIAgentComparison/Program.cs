using System.ClientModel;
using AgentFrameworkToolkit.AzureOpenAI;
using AgentFrameworkToolkit.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Responses;
using Shared;

#pragma warning disable OPENAI001

Utils.Init("AFT: Extension Methods");

const string prompt = "What is the capital of France? Answer in one sentence.";

Utils.Green("1. Agent Framework alone");
await AgentFrameworkAlone.RunAsync(prompt);

Utils.Separator();
Utils.Green("2. Agent Framework Toolkit factory");
await AgentFrameworkToolkitFactory.RunAsync(prompt);

Utils.Separator();
Utils.Green("3. Agent Framework Toolkit AsAIAgent extension");
await AgentFrameworkToolkitExtensionMethod.RunAsync(prompt);

public static class AgentFrameworkAlone
{
    public static async Task RunAsync(string prompt)
    {
        Secrets secrets = SecretsManager.GetSecrets();
        OpenAIClient client = new(new ApiKeyCredential(secrets.AzureOpenAiKey), new OpenAIClientOptions
        {
            Endpoint = new Uri($"{secrets.AzureOpenAiEndpoint}openai/v1")
        });

        AIAgent agent = client.GetResponsesClient().AsAIAgent(
            model: "gpt-6-luna",
            options: new ChatClientAgentOptions
            {
                ChatOptions = new ChatOptions
                {
                    RawRepresentationFactory = _ => new CreateResponseOptions
                    {
                        ReasoningOptions = new ResponseReasoningOptions
                        {
                            ReasoningEffortLevel = ResponseReasoningEffortLevel.High,
                            ReasoningSummaryVerbosity = ResponseReasoningSummaryVerbosity.Detailed
                        }
                    }
                }
            });

        AgentResponse response = await agent.RunAsync(prompt);
        Console.WriteLine(response);
    }
}

public static class AgentFrameworkToolkitFactory
{
    public static async Task RunAsync(string prompt)
    {
        Secrets secrets = SecretsManager.GetSecrets();
        AzureOpenAIAgentFactory factory = new(secrets.AzureOpenAiEndpoint, secrets.AzureOpenAiKey);

        AzureOpenAIAgent agent = factory.CreateAgent(new AgentOptions
        {
            ClientType = ClientType.ResponsesApi,
            Model = OpenAIChatModels.Gpt6Luna,
            ReasoningEffort = OpenAIReasoningEffort.High,
            ReasoningSummaryVerbosity = OpenAIReasoningSummaryVerbosity.Detailed
        });

        AgentResponse response = await agent.RunAsync(prompt);
        Console.WriteLine(response);
    }
}

public static class AgentFrameworkToolkitExtensionMethod
{
    public static async Task RunAsync(string prompt)
    {
        Secrets secrets = SecretsManager.GetSecrets();
        OpenAIClient client = new(new ApiKeyCredential(secrets.AzureOpenAiKey), new OpenAIClientOptions
        {
            Endpoint = new Uri($"{secrets.AzureOpenAiEndpoint}openai/v1")
        });

        AIAgent agent = client.AsAIAgent(new AgentOptions
        {
            Model = OpenAIChatModels.Gpt6Luna,
            ReasoningEffort = OpenAIReasoningEffort.High,
            ReasoningSummaryVerbosity = OpenAIReasoningSummaryVerbosity.Detailed
        });

        AgentResponse response = await agent.RunAsync(prompt);
        Console.WriteLine(response);
    }
}