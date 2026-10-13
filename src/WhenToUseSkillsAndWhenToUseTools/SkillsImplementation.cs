using AgentFrameworkToolkit.AzureOpenAI;
using AgentFrameworkToolkit.OpenAI;
using Microsoft.Agents.AI;
using Shared;

namespace WhenToUseSkillsAndWhenToUseTools;

public static class SkillsImplementation
{
    public static async Task Run(AzureOpenAIAgentFactory agentFactory)
    {
        AgentInlineSkill skill = new(
            name: "weather-skills",
            description: "Use for weather-related stuff",
            instructions: """
                          Use 'get-weather-for-city' to get the weather
                          Use resource 'how-we-predict-weather' to tell user how weather is predicted
                          Use resource 'who-is-in-charge-of-the-weather' to tell who is in charge
                          """);
        skill.AddScript("get-weather-for-city", WeatherStuff.GetWeatherForCity);
        skill.AddResource("how-we-predict-weather", "We look out the window and see it it rain or is sunny");
        skill.AddResource("who-is-in-charge-of-the-weather", "Some odd guy in an orange sweater");

        AgentSkillsProvider provider = new([skill], options: new AgentSkillsProviderOptions
        {
            DisableLoadSkillApproval = true,
            DisableReadSkillResourceApproval = true,
            DisableRunSkillScriptApproval = true
        });

        AIAgent agent = agentFactory.CreateAgent(new AgentOptions
        {
            Model = "gpt-6-luna",
            AIContextProviders = [provider],
            RawHttpCallDetails = details => { Console.WriteLine(details.RequestData); },
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
}