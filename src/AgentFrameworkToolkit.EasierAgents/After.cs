using AgentFrameworkToolkit.AzureOpenAI;
using AgentFrameworkToolkit.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Shared;
using Shared.Extensions;

namespace AgentFrameworkToolkit.EasierAgents;

public class After
{
    public static async Task RunAsync()
    {
        //Weather-Task: With gpt-6-luna in low Reasoning, Call Tool with middleware and return as structured Output

        Secrets secrets = SecretsManager.GetSecrets();

        AzureOpenAIAgentFactory agentFactory = new(secrets.AzureOpenAiEndpoint, secrets.AzureOpenAiKey);
        agentFactory.Connection.DefaultClientType = global::AgentFrameworkToolkit.OpenAI.ClientType.ResponsesApi;

        AzureOpenAIAgent agent = agentFactory.CreateAgent(new AgentOptions
        {
            Model = OpenAIChatModels.Gpt6Luna,
            ReasoningEffort = OpenAIReasoningEffort.Low,
            Tools = [AIFunctionFactory.Create(WeatherTool.GetWeather)],
            RawToolCallDetails = details => { Utils.Gray(details.ToString()); }
        });

        AgentResponse<WeatherReport> response = await agent.RunAsync<WeatherReport>("What is the Weather like in Paris");
        WeatherReport weatherReport = response.Result;
        Console.WriteLine("City: " + weatherReport.City);
        Console.WriteLine("Condition: " + weatherReport.Condition);
        Console.WriteLine("Degrees: " + weatherReport.Degrees);
        Console.WriteLine("Fahrenheit: " + weatherReport.Fahrenheit);
        response.Usage.OutputAsInformation();
    }
}