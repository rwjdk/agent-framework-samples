#pragma warning disable OPENAI001
using OpenAI.Responses;
//YouTube video that cover this sample: https://youtu.be/VInKZ45YKAM


using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using Shared;
using System.ClientModel;
using System.Text.Json;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;
#pragma warning disable MAAIW001

Secrets secrets = SecretsManager.GetSecrets();

OpenAIClient client = ClientHelper.GetAzureOpenAIClient();

ChatClientAgent intentAgent = client.GetResponsesClient().AsIChatClient("gpt-6-luna").AsAIAgent(name: "IntentAgent", instructions: "Determine what type of question was asked. Never answer yourself");

ChatClientAgent movieNerd = client.GetResponsesClient().AsIChatClient("gpt-6-luna").AsAIAgent(name: "MovieNerd", instructions: "You are a Movie Nerd");
ChatClientAgent musicNerd = client.GetResponsesClient().AsIChatClient("gpt-6-luna").AsAIAgent(name: "MusicNerd", instructions: "You are a Music Nerd");


while (true)
{
    List<ChatMessage> messages = [];
    Workflow workflow = AgentWorkflowBuilder.CreateHandoffBuilderWith(intentAgent)
        .WithHandoffs(intentAgent, [movieNerd, musicNerd])
        .WithHandoffs([movieNerd, musicNerd], intentAgent)
        .Build();
    Console.Write("> ");
    messages.Add(new(ChatRole.User, Console.ReadLine()!));
    messages.AddRange(await RunWorkflowAsync(workflow, messages));
}

static async Task<List<ChatMessage>> RunWorkflowAsync(Workflow workflow, List<ChatMessage> messages)
{
    string? lastExecutorId = null;

    StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, messages);
    await run.TrySendMessageAsync(new TurnToken(emitEvents: true));
    await foreach (WorkflowEvent @event in run.WatchStreamAsync())
    {
        switch (@event)
        {
            case AgentResponseUpdateEvent e:
            {
                if (e.ExecutorId != lastExecutorId)
                {
                    lastExecutorId = e.ExecutorId;
                    Console.WriteLine();
                    Utils.Green(e.Update.AuthorName ?? e.ExecutorId);
                }

                Console.Write(e.Update.Text);
                if (e.Update.Contents.OfType<FunctionCallContent>().FirstOrDefault() is FunctionCallContent call)
                {
                    Console.WriteLine();
                    Utils.Gray($"Call '{call.Name}' with arguments: {JsonSerializer.Serialize(call.Arguments)}]");
                }

                break;
            }
            case WorkflowOutputEvent output:
                Utils.Separator();
                return output.As<List<ChatMessage>>()!;
            case ExecutorFailedEvent failedEvent:
                if (failedEvent.Data is Exception ex)
                {
                    Utils.Red($"Error in agent {failedEvent.ExecutorId}: " + ex);
                }

                break;
        }
    }

    return [];
}