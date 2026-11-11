using AgentFrameworkToolkit.AzureOpenAI.Batching;
using AgentFrameworkToolkit.OpenAI.Batching;
using JetBrains.Annotations;
using Microsoft.Extensions.AI;
using Shared;
#pragma warning disable AFT999

namespace AgentFrameworkToolkit.EasierBatching.Scenarios;

public static class AzureOpenAIChatStructuredOutput
{
    [UsedImplicitly]
    private class CityInfo
    {
        public required string City { get; set; }
        public required int PopulationInMillion { get; set; }
    }

    public static async Task RunSampleAsync(Secrets secrets)
    {
        AzureOpenAIBatchRunner batchRunner = new(secrets.AzureOpenAiEndpoint, secrets.AzureOpenAiKey);

        ChatBatchRun<CityInfo> run = await batchRunner.RunChatBatchAsync<CityInfo>(new ChatBatchOptions
            {
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

        while (run.Status != BatchRunStatus.Completed)
        {
            run = await batchRunner.GetChatBatchAsync<CityInfo>(run.Id);
            Console.WriteLine(run.Status + $" [Total: {run.Counts.Total} - Completed: {run.Counts.Completed} - Failed: {run.Counts.Failed}]");
            await Task.Delay(10_000);
        }

        IList<ChatBatchRunResult<CityInfo>> results = await run.GetResultAsync();

        foreach (ChatBatchRunResult<CityInfo> result in results)
        {
            if (result.Error == null)
            {
                //Success
                foreach (ChatMessage requestMessage in result.RequestMessages)
                {
                    Utils.Gray(requestMessage.Text);
                }

                Utils.Green(result.ResponseObject!.City ?? "<NoText>");
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