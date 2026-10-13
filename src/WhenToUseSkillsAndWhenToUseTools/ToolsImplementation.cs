using AgentFrameworkToolkit.AzureOpenAI;
using AgentFrameworkToolkit.OpenAI;
using AgentFrameworkToolkit.Tools;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Shared;

namespace WhenToUseSkillsAndWhenToUseTools;

public static class ToolsImplementation
{
    public static async Task Run(AzureOpenAIAgentFactory agentFactory)
    {
        List<AITool> tools = [];

        int multiplier = 10;
        for (int i = 0; i < multiplier; i++)
        {
            tools.Add(AIFunctionFactory.Create(WeatherStuff.GetWeatherForCity, $"get-weather-for-city-{i}", "Get the weather for a given city"));
            tools.Add(AIFunctionFactory.Create(HowToPredictWeather, $"how-we-predict-weather-{i}", "How we predict the Weather"));
            tools.Add(AIFunctionFactory.Create(WhoIsInCharge, $"who-is-in-charge-of-the-weather-{i}", "Who is in charge of the weather"));
        }

        AIAgent agent = agentFactory.CreateAgent(new AgentOptions
        {
            Model = "gpt-6-luna",
            Tools = tools,
            //RawHttpCallDetails = details => { Console.WriteLine(details.RequestData); },
            RawToolCallDetails = Console.WriteLine
        });

        const string q1 = "Hi";
        AgentResponse response1 = await agent.RunAsync(q1);
        Utils.Green(q1);
        Console.WriteLine(response1);
        Console.WriteLine("Input Tokens: " + response1.Usage!.InputTokenCount!.Value);

        Console.WriteLine("---");

        const string q2 = "How is the Weather Predicted?";
        Utils.Green(q2);
        AgentResponse response2 = await agent.RunAsync(q2);
        Console.WriteLine(response2);
        Console.WriteLine("Input Tokens: " + response2.Usage!.InputTokenCount!.Value);

        Console.WriteLine("---");

        const string q3 = "What is the Weather like in Paris";
        Utils.Green(q3);
        AgentResponse response3 = await agent.RunAsync(q3);
        Console.WriteLine(response3);
        Console.WriteLine("Input Tokens: " + response3.Usage!.InputTokenCount!.Value);
    }

    public static string HowToPredictWeather()
    {
        return "We look out the window and see it it rain or is sunny";
    }
    
    public static string WhoIsInCharge()
    {
        return "Some odd guy in an orange sweater";
    }
}


