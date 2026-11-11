//YouTube video that cover this sample: https://youtu.be/4D02zSl4QAQ

using System.ClientModel;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Responses;
using Shared;

#pragma warning disable OPENAI001
Secrets secrets = SecretsManager.GetSecrets();

//OpenAIClient client = new(secrets.OpenAiApiKey);
OpenAIClient client = ClientHelper.GetAzureOpenAIClient();

ChatClientAgent agent = client
    .GetResponsesClient()
    .AsAIAgent(model: "gpt-6-luna", options: new ChatClientAgentOptions
    {
        ChatOptions = new ChatOptions
        {
            RawRepresentationFactory = _ => new CreateResponseOptions //<--- Notice this is different from out ChatCompletionOptions
            {
                ReasoningOptions = new ResponseReasoningOptions
                {
                    ReasoningEffortLevel = ResponseReasoningEffortLevel.Medium,
                    ReasoningSummaryVerbosity = ResponseReasoningSummaryVerbosity.Detailed
                }
            }
        }
    });

AgentResponse response = await agent.RunAsync("What is the capital of france and how many live there?");

foreach (ChatMessage message in response.Messages)
{
    foreach (AIContent content in message.Contents)
    {
        if (content is TextReasoningContent textReasoningContent)
        {
            Utils.Green("The Reasoning");
            Utils.Gray(textReasoningContent.Text);
        }
    }
}

Utils.Green("The Answer");
Console.WriteLine(response);