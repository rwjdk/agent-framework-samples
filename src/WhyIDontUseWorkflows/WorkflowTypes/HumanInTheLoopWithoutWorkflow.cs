#pragma warning disable OPENAI001
using OpenAI.Responses;
using Microsoft.Extensions.AI;
using Microsoft.Agents.AI;
using OpenAI;
using OpenAI.Chat;
using Shared;
using System.ClientModel;

namespace WhyIDontUseWorkflows.WorkflowTypes;

public static class HumanInTheLoopWithoutWorkflow
{
    public static async Task Run()
    {
        Console.Clear();
        OpenAIClient client = ClientHelper.GetAzureOpenAIClient();
        IChatClient chatClient = client.GetResponsesClient().AsIChatClient("gpt-6-luna");
        ChatClientAgent agent = chatClient.AsAIAgent(instructions: "You are the judge in a guessing game where it is about guessing animals. " +
                                                                   "Each hint should only give one fact");

        List<string> animals = ["Wolf", "Eagle", "Tiger", "Dolphin", "Elephant", "Grizzly Bear", "Mouse", "Dog", "Shark", "Panda"];

        string animalToGuess = animals[Random.Shared.Next(animals.Count)];
        AgentResponse<string> initialHintResponse = await agent.RunAsync<string>($"Make a vague hint for the animal: '{animalToGuess}'");

        int numberOfTries = 0;
        IList<string> hintsGiven = [];
        Console.WriteLine("Guess what animal I'm thinking of");
        string hint = initialHintResponse.Result;
        while (true)
        {
            numberOfTries++;
            Console.WriteLine($"Hint: {hint}");
            Console.Write("Your Guess: ");
            string? guess = Console.ReadLine();
            string input = $"Is this the right answer for guessing the animal is '{animalToGuess}'? (allow for spelling errors of the animal): {guess}";
            AgentResponse<bool> isRightAnswerResponse = await agent.RunAsync<bool>(input);
            if (isRightAnswerResponse.Result)
            {
                Utils.Green($"You guessed it. " +
                                  $"The answer is indeed a {animalToGuess}. " +
                                  $"Tries used: {numberOfTries}");
                break;
            }

            //Not correct answer: Let's generate a new Hint
            string newHintPrompt = $"Generate a hint for a child to guess the animal '{animalToGuess}'. " +
                                   $"Hints already given: {string.Join(" | ", hintsGiven)} " +
                                   $"so make the new hint unique and do not repeat the same hint parts";
            AgentResponse<string> hintResponse = await agent.RunAsync<string>(newHintPrompt);
            string newHint = hintResponse.Result;
            hintsGiven.Add(newHint);
            hint = newHint;
            Console.WriteLine("That is not the animal I'm thinking of");
        }
    }
}