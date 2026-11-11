#pragma warning disable OPENAI001
using OpenAI.Responses;

using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hyperlight;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using Shared;

OpenAIClient client = ClientHelper.GetAzureOpenAIClient();
IChatClient chatClient = client.GetResponsesClient().AsIChatClient("gpt-6-luna");

await Javascript();
await JavascriptWithTools();
await Python();
await PythonNoAIContextProvider();

async Task Javascript()
{
    Utils.Yellow("JavaScript Demo");
    ChatClientAgent agent = chatClient.AsAIAgent(new ChatClientAgentOptions
    {
        ChatOptions = new ChatOptions
        {
            Instructions = "Always use use 'execute_code' tool for any math calculations " +
                           "(write the code in Javascript). Show result + Code you used"
        },
        AIContextProviders = [new HyperlightCodeActProvider()]
    });

    AgentResponse response = await agent.RunAsync("What is the 20th Fibonacci number divided by 42 and then subtract 30?");
    Console.WriteLine(response);
}

async Task JavascriptWithTools()
{
    Utils.Yellow("Javascript with Tools Demo");
    HyperlightCodeActProviderOptions options = HyperlightCodeActProviderOptions.CreateForJavaScript();
    options.Tools = [AIFunctionFactory.Create(WeatherTool.GetWeather, "get_weather_for_city")];

    ChatClientAgent agent = chatClient.AsAIAgent(new ChatClientAgentOptions
    {
        ChatOptions = new ChatOptions
        {
            Instructions = "Always use use 'execute_code' tool for any requests (write the code in Javascript). Show result + Code you used"
        },
        AIContextProviders = [new HyperlightCodeActProvider(options)]
    });

    AgentResponse response = await agent.RunAsync("What is the weather like in Paris?");
    Console.WriteLine(response);
}

async Task Python()
{
    Utils.Yellow("Python Demo");

    HyperlightCodeActProviderOptions options = HyperlightCodeActProviderOptions.CreateForWasm(modulePath: "python-sandbox.aot");
    ChatClientAgent agent = chatClient.AsAIAgent(new ChatClientAgentOptions
    {
        ChatOptions = new ChatOptions
        {
            Instructions = "Always use use 'execute_code' tool for any math calculations (write the code in Python). " +
                           "Show result + Code you used"
        },
        AIContextProviders = [new HyperlightCodeActProvider(options)]
    });

    AgentResponse response = await agent.RunAsync("What is the 20th Fibonacci number divided by 42 and then subtract 30?");
    Console.WriteLine(response);
}

async Task PythonNoAIContextProvider()
{
    Utils.Yellow("Python Demo (No AIContextProvider)");

    HyperlightCodeActProviderOptions options = HyperlightCodeActProviderOptions.CreateForWasm(modulePath: "python-sandbox.aot");

    using HyperlightExecuteCodeFunction codeFunction = new HyperlightExecuteCodeFunction(options);
    string toolInstructions = codeFunction.BuildInstructions();

    ChatClientAgent agent = chatClient.AsAIAgent(
        instructions: "Always use use 'execute_code' tool for any math calculations (write the code in Python)." +
                           " Show result + Code you used. "+ toolInstructions,
        tools: [codeFunction]
    );

    AgentResponse response = await agent.RunAsync("What is the 20th Fibonacci number divided by 42 and then subtract 30?");
    Console.WriteLine(response);
}

public static class WeatherTool
{
    public static string GetWeather(string city)
    {
        return "It is Sunny and 19 Degrees";
    }
}