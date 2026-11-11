#pragma warning disable OPENAI001
using OpenAI.Responses;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using Shared;
using System.ClientModel;
using System.Text;

namespace WhyIDontUseWorkflows.WorkflowTypes;

public static class HandoffWithoutWorkflowToolEdition
{
    public static async Task Run()
    {
        OpenAIClient client = ClientHelper.GetAzureOpenAIClient();

        ChatClientAgent movieNerd = client.GetResponsesClient().AsIChatClient("gpt-6-luna").AsAIAgent(name: "MovieNerd", instructions: "You are a Movie Nerd");
        ChatClientAgent musicNerd = client.GetResponsesClient().AsIChatClient("gpt-6-luna").AsAIAgent(name: "MusicNerd", instructions: "You are a Music Nerd");

        AIAgent intentAgent = client.GetResponsesClient().AsIChatClient("gpt-6-luna")
            .AsAIAgent(name: "IntentAgent",
                instructions: "Determine what type of question was asked and use tool. Never answer yourself",
                tools: [movieNerd.AsAIFunction(), musicNerd.AsAIFunction()]).AsBuilder().Use(FunctionCallMiddleware).Build();

        Console.Write("> ");
        string input = Console.ReadLine()!;

        AgentResponse response = await intentAgent.RunAsync(input);
        Console.WriteLine(response);
    }

    static async ValueTask<object?> FunctionCallMiddleware(AIAgent callingAgent, FunctionInvocationContext context, Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next, CancellationToken cancellationToken)
    {
        StringBuilder functionCallDetails = new();
        functionCallDetails.Append($"- Tool Call: '{context.Function.Name}'");
        if (context.Arguments.Count > 0)
        {
            functionCallDetails.Append($" (Args: {string.Join(",", context.Arguments.Select(x => $"[{x.Key} = {x.Value}]"))}");
        }

        Utils.Gray(functionCallDetails.ToString());

        return await next(context, cancellationToken);
    }
}