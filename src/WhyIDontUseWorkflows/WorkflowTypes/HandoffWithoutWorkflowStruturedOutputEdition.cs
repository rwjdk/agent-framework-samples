#pragma warning disable OPENAI001
using OpenAI.Responses;
using Microsoft.Extensions.AI;

using Microsoft.Agents.AI;
using OpenAI;
using OpenAI.Chat;
using Shared;
using System.ClientModel;

namespace WhyIDontUseWorkflows.WorkflowTypes;

public static class HandoffWithoutWorkflowStructuredOutputEdition
{
    public static async Task Run()
    {
        OpenAIClient client = ClientHelper.GetAzureOpenAIClient();

        ChatClientAgent intentAgent = client.GetResponsesClient().AsIChatClient("gpt-6-luna").AsAIAgent(name: "IntentAgent", instructions: "Determine what type of question was asked. Never answer yourself");

        ChatClientAgent movieNerd = client.GetResponsesClient().AsIChatClient("gpt-6-luna").AsAIAgent(name: "MovieNerd", instructions: "You are a Movie Nerd");
        ChatClientAgent musicNerd = client.GetResponsesClient().AsIChatClient("gpt-6-luna").AsAIAgent(name: "MusicNerd", instructions: "You are a Music Nerd");

        Console.Write("> ");
        string input = Console.ReadLine()!;

        AgentResponse<TypeOfQuestion> intentResponse = await intentAgent.RunAsync<TypeOfQuestion>(input);
        switch (intentResponse.Result)
        {
            case TypeOfQuestion.Movie:
                {
                    Utils.Green(movieNerd.Name!);
                    AgentResponse response = await movieNerd.RunAsync(input);
                    Console.WriteLine(response);
                    break;
                }
            case TypeOfQuestion.Music:
                {
                    Utils.Green(musicNerd.Name!);
                    AgentResponse response = await musicNerd.RunAsync(input);
                    Console.WriteLine(response);
                    break;
                }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}

enum TypeOfQuestion
{
    Movie,
    Music
}