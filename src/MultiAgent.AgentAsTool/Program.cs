#pragma warning disable OPENAI001
using OpenAI.Responses;
//YouTube video that cover this sample: https://youtu.be/wL4V78s_wI4


using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using MultiAgent.AgentAsTool;
using OpenAI;
using Shared;
using Shared.Extensions;
using System.ClientModel;
using System.Text;
using OpenAI.Chat;

Secrets secrets = SecretsManager.GetSecrets();

OpenAIClient client = ClientHelper.GetAzureOpenAIClient();

AIAgent stringAgent = client
    .GetResponsesClient().AsIChatClient("gpt-6-luna")
    .AsAIAgent(
        name: "StringAgent",
        instructions: "You are string manipulator",
        tools:
        [
            AIFunctionFactory.Create(StringTools.Reverse),
            AIFunctionFactory.Create(StringTools.Uppercase),
            AIFunctionFactory.Create(StringTools.Lowercase)
        ])
    .AsBuilder()
    .Use(FunctionCallMiddleware)
    .Build();

AIAgent numberAgent = client
    .GetResponsesClient().AsIChatClient("gpt-6-luna")
    .AsAIAgent(
        name: "NumberAgent",
        instructions: "You are a number expert",
        tools:
        [
            AIFunctionFactory.Create(NumberTools.RandomNumber),
            AIFunctionFactory.Create(NumberTools.AnswerToEverythingNumber)
        ])
    .AsBuilder()
    .Use(FunctionCallMiddleware) //Middleware
    .Build();

Utils.Green("DELEGATE AGENT");

AIAgent delegationAgent = client
    .GetResponsesClient().AsIChatClient("gpt-6-luna")
    .AsAIAgent(
        name: "DelegateAgent",
        instructions: "Are a Delegator of String and Number Tasks. Never does such work yourself",
        tools:
        [
            stringAgent.AsAIFunction(new AIFunctionFactoryOptions
            {
                Name = "StringAgentAsTool"
            }),
            numberAgent.AsAIFunction(new AIFunctionFactoryOptions
            {
                Name = "NumberAgentAsTool"
            })
        ]
    )
    .AsBuilder()
    .Use(FunctionCallMiddleware)
    .Build();

AgentResponse responseFromDelegate = await delegationAgent.RunAsync("Uppercase 'Hello World'");
Console.WriteLine(responseFromDelegate);
responseFromDelegate.Usage.OutputAsInformation();

Utils.Separator();

Utils.Green("JACK OF ALL TRADE AGENT");

AIAgent jackOfAllTradesAgent = client
    .GetResponsesClient().AsIChatClient("gpt-6-luna")
    .AsAIAgent(
        name: "JackOfAllTradesAgent",
        instructions: "Are a Agent that can answer questions on strings and numbers",
        tools:
        [
            AIFunctionFactory.Create(StringTools.Reverse),
            AIFunctionFactory.Create(StringTools.Uppercase),
            AIFunctionFactory.Create(StringTools.Lowercase),
            AIFunctionFactory.Create(NumberTools.RandomNumber),
            AIFunctionFactory.Create(NumberTools.AnswerToEverythingNumber)
        ]
    )
    .AsBuilder()
    .Use(FunctionCallMiddleware)
    .Build();

AgentResponse responseFromJackOfAllTrade = await jackOfAllTradesAgent.RunAsync("Uppercase 'Hello World'");
Console.WriteLine(responseFromJackOfAllTrade);
responseFromJackOfAllTrade.Usage.OutputAsInformation();

async ValueTask<object?> FunctionCallMiddleware(AIAgent callingAgent, FunctionInvocationContext context, Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next, CancellationToken cancellationToken)
{
    StringBuilder functionCallDetails = new();
    functionCallDetails.Append($"- Tool Call: '{context.Function.Name}' [Agent: {callingAgent.Name}]");
    if (context.Arguments.Count > 0)
    {
        functionCallDetails.Append($" (Args: {string.Join(",", context.Arguments.Select(x => $"[{x.Key} = {x.Value}]"))}");
    }

    Utils.Gray(functionCallDetails.ToString());

    return await next(context, cancellationToken);
}