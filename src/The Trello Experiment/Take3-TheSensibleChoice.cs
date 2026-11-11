
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using Shared;
using System.Text;
using OpenAI.Responses;
using The_Trello_Experiment.Tools;
using TrelloDotNet;

#pragma warning disable OPENAI001

namespace The_Trello_Experiment;

public static class Take3TheSensibleChoice
{
    public static async Task Run(OpenAIClient OpenAIClient, string trelloApiKey, string trelloToken)
    {
        TrelloTools trelloTools = new(new TrelloClient(trelloApiKey, trelloToken));

        AIAgent agent = OpenAIClient
            .GetResponsesClient()
            .AsAIAgent(
                model: "gpt-6-luna",
                instructions: """
                              You are a Trello Expert

                              Use the various tools to get Trello Information and do Actions
                              """,
                tools:
                [
                    AIFunctionFactory.Create(trelloTools.AddNewCard),
                    AIFunctionFactory.Create(trelloTools.GetBoards),
                    AIFunctionFactory.Create(trelloTools.GetCardsOnBoard),
                    AIFunctionFactory.Create(trelloTools.GetLabelsOnBoard),
                    AIFunctionFactory.Create(trelloTools.GetListsOnBoard),
                    AIFunctionFactory.Create(trelloTools.MoveCard),
                ]
            ).AsBuilder().Use(FunctionCallMiddleware).Build();

        AgentSession session = await agent.CreateSessionAsync();

        while (true)
        {
            Console.Write("> ");
            string? input = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(input))
            {
                ChatMessage message = new(ChatRole.User, input);
                await foreach (AgentResponseUpdate update in agent.RunStreamingAsync(message, session))
                {
                    Console.Write(update);
                }
            }

            Utils.Separator();
        }
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