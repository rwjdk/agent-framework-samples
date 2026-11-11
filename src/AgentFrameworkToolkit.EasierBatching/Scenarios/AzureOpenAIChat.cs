using AgentFrameworkToolkit.AzureOpenAI.Batching;
using AgentFrameworkToolkit.OpenAI.Batching;
using Microsoft.Extensions.AI;
using Shared;

#pragma warning disable AFT999

namespace AgentFrameworkToolkit.EasierBatching.Scenarios;

public static class AzureOpenAIChat
{
    public static async Task RunSampleAsync(Secrets secrets)
    {
        AzureOpenAIBatchRunner batchRunner = new(secrets.AzureOpenAiEndpoint, secrets.AzureOpenAiKey);

        ChatBatchRun run = await batchRunner.RunChatBatchAsync(new ChatBatchOptions
            {
                WaitUntilCompleted = true,
                ClientType = ChatBatchClientType.ResponsesApi,
                Model = "gpt-6-luna",
                Instructions = "You are a Capital Expert",
                //ReasoningEffort = OpenAIReasoningEffort.Low,
                //ReasoningSummaryVerbosity = OpenAIReasoningSummaryVerbosity.Detailed
        },
            [
                ChatBatchRequest.Create("What is the Capital of France?"),
                ChatBatchRequest.Create("What is the Capital of Australia?")
            ]
        );

        IList<ChatBatchRunResult> results = await run.GetResultAsync(cleanUpRemoteFilesOnSuccessfulRetrieval: false);

        foreach (ChatBatchRunResult result in results)
        {
            if (result.Error == null)
            {
                //Success
                foreach (ChatMessage requestMessage in result.RequestMessages)
                {
                    Utils.Gray(requestMessage.Text);
                }

                Utils.Green(result.ResponseMessage?.Text ?? "<NoText>");
            }
            else
            {
                //Error
                Utils.Red(result.ResponseMessage!.Text);
            }
            Utils.Separator();
        }
    }
}