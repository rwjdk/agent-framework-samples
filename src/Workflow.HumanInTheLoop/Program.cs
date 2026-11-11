#pragma warning disable OPENAI001
using OpenAI.Responses;
using Microsoft.Extensions.AI;

using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using OpenAI;
using OpenAI.Chat;
using Shared;
using System.ClientModel;

Console.Clear();
Secrets secrets = SecretsManager.GetSecrets();
OpenAIClient client = ClientHelper.GetAzureOpenAIClient();
IChatClient chatClient = client.GetResponsesClient().AsIChatClient("gpt-6-luna");
ChatClientAgent agent = chatClient.AsAIAgent(instructions: "You are the judge in a guessing game where it is about guessing animals. " +
                                                           "Each hint should only give one fact");

List<string> animals = ["Wolf", "Eagle", "Tiger", "Dolphin", "Elephant", "Grizzly Bear", "Mouse", "Dog", "Shark", "Panda"];

string animalToGuess = animals[Random.Shared.Next(animals.Count)] ;
AgentResponse<string> initialHintResponse = await agent.RunAsync<string>($"Make a vague hint for the animal: '{animalToGuess}'");

RequestPort requestPort = RequestPort.Create<FeedbackToUser, string>("GuessAnimal");
EvaluateAndHintExecutor evaluateAndHintExecutor = new EvaluateAndHintExecutor(agent, animalToGuess);
Workflow workflow = new WorkflowBuilder(requestPort)
    .AddEdge(requestPort, evaluateAndHintExecutor)
    .AddEdge(evaluateAndHintExecutor, requestPort)
    .WithOutputFrom(evaluateAndHintExecutor)
    .Build();

FeedbackToUser initialFeedback = new FeedbackToUser(initialHintResponse.Result, true);
await using StreamingRun handle = await InProcessExecution.RunStreamingAsync(workflow, initialFeedback);

await foreach (WorkflowEvent evt in handle.WatchStreamAsync())
{
    switch (evt)
    {
        case RequestInfoEvent requestInputEvt:
            ExternalRequest externalRequest = requestInputEvt.Request;
            if (externalRequest.IsDataOfType<FeedbackToUser>())
            {
                FeedbackToUser feedbackToUser = externalRequest.Data.As<FeedbackToUser>()!;
                Utils.Gray(feedbackToUser.Init ?
                    "Guess what animal I'm thinking of" :
                    "That is not the animal I'm thinking of");
                Console.WriteLine($"Hint: {feedbackToUser.Hint}");
                Utils.Separator();
                Console.Write("Your Guess: ");
                string? input = Console.ReadLine();
                ExternalResponse externalResponse = externalRequest.CreateResponse(input);
                await handle.SendResponseAsync(externalResponse);
                break;
            }
            throw new NotSupportedException($"Request {externalRequest.PortInfo.RequestType} is not supported");
        case WorkflowOutputEvent outputEvt:
            Utils.Green(outputEvt.Data!.ToString()!);
            return;
    }
}

[YieldsOutput(typeof(string))]
[SendsMessage(typeof(FeedbackToUser))]
class EvaluateAndHintExecutor(ChatClientAgent agent, string animalToGuess) : Executor<string>("Evaluator and Hint-giver")
{
    private int _numberOfTries;
    private readonly IList<string> _hintsGiven = [];

    public override async ValueTask HandleAsync(
        string message,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        _numberOfTries++;
        string input = $"Is this the right answer for guessing the animal is '{animalToGuess}'? (allow for spelling errors of the animal): {message}";
        AgentResponse<bool> isRightAnswerResponse = await agent.RunAsync<bool>(input, cancellationToken: cancellationToken);
        if (isRightAnswerResponse.Result)
        {
            //End the Game
            await context.YieldOutputAsync($"You guessed it. " +
                                           $"The answer is indeed a {animalToGuess}. " +
                                           $"Tries used: {_numberOfTries}", cancellationToken);
        }
        else
        {
            //Not correct answer: Let's generate a new Hint
            string newHintPrompt = $"Generate a hint for a child to guess the animal '{animalToGuess}'. " +
                                   $"Hints already given: {string.Join(" | ", _hintsGiven)} " +
                                   $"so make the new hint unique and do not repeat the same hint parts";
            AgentResponse<string> hintResponse = await agent.RunAsync<string>(newHintPrompt, cancellationToken: cancellationToken);
            string newHint = hintResponse.Result;
            _hintsGiven.Add(newHint);
            await context.SendMessageAsync(new FeedbackToUser(newHint), cancellationToken);
        }
    }
}

record FeedbackToUser(string Hint, bool Init = false);
