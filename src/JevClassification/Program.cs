#pragma warning disable OPENAI001
using Microsoft.Extensions.AI;
using System.Diagnostics;
using System.Text.Json;
using JevClassification;
using JevClassification.Models;
using Microsoft.Agents.AI;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Responses;
using Shared;
// ReSharper disable VariableHidesOuterVariable
#pragma warning disable OPENAI001

int numberOfMessages = 5;
Utils.Init($"Classifying {numberOfMessages} messages (LLM vs Jev)");
List<UserMessage> messages = SampleMessages.GetListOfMessages(numberOfMessages);

foreach (UserMessage message in messages)
{
    Utils.Gray($"{message.Id}: {message.Text}");
}
Utils.LineSeparator();
Utils.Green("1. MAF (5.6-sol) - One message at the time via Structured Output");
await AgentFrameworkOneByOne(messages);

Utils.LineSeparator();
Utils.Green("2. MAF (5.6-sol) - Batched via Structured Output");
await AgentFrameworkBatched(messages);

Utils.LineSeparator();
Utils.Green("3. Jev Classification mechanism");
await Jev(messages);

Console.ReadLine();

async Task AgentFrameworkOneByOne(List<UserMessage> userMessages)
{
    WaitForKeyPressToContinue();
    Stopwatch stopwatch = Stopwatch.StartNew();
    OpenAIClient openAIClient = ClientHelper.GetAzureOpenAIClient();
    ChatClientAgent agent = openAIClient
        .GetResponsesClient()
        .AsAIAgent(
            model: "gpt-6-luna",
            instructions: """
                          Classify user message using only the categories in the response schema.
                          Use the full -100 to 100 sentiment scale and make the sentiment label agree with the score.
                          """);

    List<MessageClassification> result = [];
    long inputTokenCount = 0;
    long outputTokenCount = 0;
    foreach (UserMessage message in userMessages)
    {
        AgentResponse<MessageClassification> response = await agent.RunAsync<MessageClassification>($"Id: {message.Id} - Text: {message.Text}");
        result.Add(response.Result);
        inputTokenCount += response.Usage!.InputTokenCount!.Value;
        outputTokenCount += response.Usage!.OutputTokenCount!.Value;
    }
    decimal price = (inputTokenCount / 1_000_000.0M * 4.0M) + (outputTokenCount / 1_000_000.0M * 20.0M);
    ListResults(result, stopwatch, inputTokenCount, outputTokenCount, price);
}

async Task AgentFrameworkBatched(List<UserMessage> userMessages)
{
    WaitForKeyPressToContinue();
    Stopwatch stopwatch = Stopwatch.StartNew();
    OpenAIClient openAIClient = ClientHelper.GetAzureOpenAIClient();
    ChatClientAgent agent = openAIClient
        .GetResponsesClient().AsIChatClient("gpt-6-luna")
        .AsAIAgent(instructions: """
                                 Classify user messages using only the categories in the response schema.
                                 Return exactly one result per message in input order. Do not combine messages.
                                 Use the full -100 to 100 sentiment scale and make the sentiment label agree with the score.
                                 """);

    string prompt = "Classify these messages:\n" + JsonSerializer.Serialize(userMessages);
    AgentResponse<ClassificationBatch> response = await agent.RunAsync<ClassificationBatch>(prompt);

    List<MessageClassification> classifications = response.Result.Classifications;
    long inputTokenCount = response.Usage!.InputTokenCount!.Value;
    long outputTokenCount = response.Usage!.OutputTokenCount!.Value;
    decimal price = (inputTokenCount / 1_000_000.0M * 4.0M) + (outputTokenCount / 1_000_000.0M * 20.0M);
    ListResults(classifications, stopwatch, inputTokenCount, outputTokenCount, price);
}

async Task Jev(List<UserMessage> userMessages)
{
    WaitForKeyPressToContinue();
    Stopwatch stopwatch = Stopwatch.StartNew();
    string typeSafeApiKey = SecretsManager.GetSecrets().TypeSafeApiKey;
    Dictionary<string, object> questions = [];
    foreach (UserMessage message in userMessages)
    {
        string messageId = message.Id;
        questions[$"{messageId}_sentiment"] = new
        {
            type = "score",
            instructions = $"Rate the sentiment expressed by the message whose id is '{message.Id}'.",
            criteria = new[] { "VeryNegative", "Negative", "Neutral", "Positive", "VeryPositive" }
        };

        questions[$"{messageId}_type"] = new
        {
            type = "choice",
            instructions = $"Classify the form of the message whose id is '{message.Id}'.",
            criteria = new Dictionary<string, string>
            {
                ["Review"] = "An evaluation of an experience or product",
                ["Comment"] = "A statement or observation that is not primarily a review",
                ["Question"] = "A request for information",
                ["SupportRequest"] = "A request for help resolving a problem",
                ["Other"] = "None of the other categories"
            }
        };

        questions[$"{messageId}_topic"] = new
        {
            type = "choice",
            instructions = $"Classify the main topic of the message whose id is '{message.Id}'.",
            criteria = new Dictionary<string, string>
            {
                ["ProductQuality"] = "Quality, usability, reliability, or performance of a product",
                ["Shipping"] = "Delivery timing, packaging, or shipment handling",
                ["Billing"] = "Charges, payments, invoices, refunds, or subscriptions",
                ["FeatureRequest"] = "A suggestion for new or changed functionality",
                ["CustomerService"] = "An experience with support or service staff",
                ["Other"] = "None of the other categories"
            }
        };

        questions[$"{messageId}_urgent"] = new
        {
            type = "noul",
            instructions = $"Does the message whose id is '{message.Id}' require urgent attention?",
            criteria = new Dictionary<string, string>
            {
                ["true"] = "Delay is likely to cause immediate material harm or the sender explicitly conveys urgency",
                ["false"] = "The message can follow the normal handling process"
            }
        };
    }

    JevClient jevClient = new(typeSafeApiKey);
    JevResponse jevResponse = await jevClient.EvaluateAsync(new { userMessages }, questions);

    if (userMessages.Count <= 10)
    {
        foreach (UserMessage message in userMessages)
        {
            JsonElement sentiment = jevResponse.Answers[$"{message.Id}_sentiment"];
            JsonElement type = jevResponse.Answers[$"{message.Id}_type"];
            JsonElement topic = jevResponse.Answers[$"{message.Id}_topic"];
            JsonElement urgent = jevResponse.Answers[$"{message.Id}_urgent"];

            double score = sentiment.GetProperty("score").GetDouble();
            double normalizedSentiment = (score - 2) * 50;

            JsonProperty strongestSentiment = sentiment
                .GetProperty("probabilities")
                .EnumerateObject()
                .MaxBy(probability => probability.Value.GetDouble());

            string sentimentLabel = sentiment
                .GetProperty("legend")
                .GetProperty(strongestSentiment.Name)
                .GetString()!;

            double urgentPercentage = urgent.GetProperty("noul").GetDouble();

            Console.WriteLine(
                $"{message.Id}: {sentimentLabel} ({normalizedSentiment:+0.0;-0.0;0}) " +
                $"{type.GetProperty("choice").GetString()}, " +
                $"{topic.GetProperty("choice").GetString()}, " +
                $"urgent: {(urgentPercentage > 0.5 ? "True" :"False")}");
        }
    }

    int inputTokenCount = jevResponse.Usage.InputTokens;
    int outputTokenCount = jevResponse.Usage.OutputTokens;
    decimal price = (inputTokenCount / 1_000_000.0M * 0.0042M) + (outputTokenCount / 1_000_000.0M * 0M);
    Console.WriteLine($"Elapsed: {stopwatch.ElapsedMilliseconds} ms | Token Usage: {inputTokenCount} In - {outputTokenCount} Out | Price {Math.Round(price, 5)}USD");
}

void ListResults(List<MessageClassification> messageClassifications, Stopwatch stopwatch, long tokensIn, long tokensOut, decimal price)
{
    if (messageClassifications.Count <= 10)
    {
        //Write message details
        foreach (MessageClassification classification in messageClassifications)
        {
            Console.WriteLine(
                $"{classification.MessageId}: {classification.Sentiment} " +
                $"({classification.SentimentScore:+0.0;-0.0;0}), {classification.MessageType}, " +
                $"{classification.Topic}, urgent:{classification.RequiresUrgentAttention}");
        }
    }
    Console.WriteLine($"Elapsed: {stopwatch.ElapsedMilliseconds} ms | TokenUsage: {tokensIn} In - {tokensOut} Out | Price {Math.Round(price, 5)}USD");
}

void WaitForKeyPressToContinue()
{
    const string message = "Press any key to continue...";
    Console.Write(message);
    Console.ReadKey(intercept: true);
    Console.Write("\r" + new string(' ', message.Length) + "\r");
}