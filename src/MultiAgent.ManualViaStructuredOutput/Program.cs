#pragma warning disable OPENAI001
using OpenAI.Responses;
using Microsoft.Extensions.AI;
//YouTube video that cover this sample: https://youtu.be/2YzjRZTZxUo

using System.ClientModel;
using System.ComponentModel;

using Microsoft.Agents.AI;
using OpenAI;
using OpenAI.Chat;
using Shared;

Secrets secrets = SecretsManager.GetSecrets();

OpenAIClient client = ClientHelper.GetAzureOpenAIClient();

IChatClient chatClientMini = client.GetResponsesClient().AsIChatClient("gpt-6-luna");
IChatClient chatClient = client.GetResponsesClient().AsIChatClient("gpt-6-luna");

Console.Write("> ");
string question = Console.ReadLine()!;

//Determine initial intent
ChatClientAgent intentAgent = chatClientMini.AsAIAgent(name: "IntentAgent", instructions: "Determine what type of question was asked. Never answer yourself");

AgentResponse<IntentResult> initialResponse = await intentAgent.RunAsync<IntentResult>(question);
IntentResult intentResult = initialResponse.Result;

//Branch out based on Intent
switch (intentResult.Intent)
{
    case Intent.MusicQuestion:
        Utils.Green("Music Question");
        ChatClientAgent musicNerdAgent = chatClient.AsAIAgent(name: "MusicNerd", instructions: "You are a Music Nerd answering questions (Give a question on max 200 chars)");
        AgentResponse responseFromMusicNerd = await musicNerdAgent.RunAsync(question);
        Console.WriteLine(responseFromMusicNerd);
        break;
    case Intent.MovieQuestion:
        Utils.Green("Movie Question");
        ChatClientAgent movieNerdAgent = chatClient.AsAIAgent(name: "MovieNerd", instructions: "You are a Movie Nerd answering questions (Give a question on max 200 chars)");
        AgentResponse responseFromMovieNerd = await movieNerdAgent.RunAsync(question);
        Console.WriteLine(responseFromMovieNerd);
        break;
    case Intent.Other:
        Utils.Green("Other Question");
        //Let Intent agent answer itself
        AgentResponse otherResponse = await intentAgent.RunAsync(question);
        Console.WriteLine(otherResponse);
        break;
    default:
        throw new ArgumentOutOfRangeException();
}

Console.ReadKey();

public class IntentResult
{
    [Description("What type of question is this?")]
    public required Intent Intent { get; set; }
}

public enum Intent
{
    MusicQuestion,
    MovieQuestion,
    Other
}