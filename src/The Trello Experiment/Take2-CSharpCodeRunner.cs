
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Responses;
using Shared;
using The_Trello_Experiment.Tools;

#pragma warning disable OPENAI001

namespace The_Trello_Experiment;

public static class Take2CSharpCodeRunner
{
    public static async Task Run(OpenAIClient OpenAIClient, string trelloApiKey, string trelloToken)
    {
        ChatClientAgent agent = OpenAIClient
            .GetResponsesClient()
            .AsAIAgent(
                model: "gpt-6-luna",
                instructions: $"""
                               You are a Trello Expert with access to the Trello and the API

                               Here is the credentials:
                               - API Key: {trelloApiKey}
                               - Token: {trelloToken}

                               Use the 'compile_and_execute_csharp_code' tool to talk via the API to Trello using C# Code... The code should be the pure code (no class, namespace, and usings around it). It need to return a string for you to get data back

                               Example of valid input to the tool:

                               string cSharpCode = @"
                                   var client = new HttpClient();
                                   var response = await client.GetStringAsync(""https://api.trello.com/..."");
                                   return response;
                               ";
                               """,
                tools: [AIFunctionFactory.Create(CSharpCodeRunnerTool.ExecuteAndReturnAsync, "compile_and_execute_csharp_code")]
            );

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
}