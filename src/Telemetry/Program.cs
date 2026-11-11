#pragma warning disable OPENAI001
using OpenAI.Responses;
using Microsoft.Extensions.AI;
//YouTube video that cover this sample: https://youtu.be/jeVQo75KcCw


using Microsoft.Agents.AI;
using OpenAI;
using OpenTelemetry;
using Shared;
using System.ClientModel;
using Azure.Monitor.OpenTelemetry.Exporter;
using OpenAI.Chat;
using OpenTelemetry.Trace;

Secrets secrets = SecretsManager.GetSecrets();

//Setup Telemetry
string sourceName = "AiSource";
TracerProviderBuilder tracerProviderBuilder = Sdk.CreateTracerProviderBuilder()
    .AddSource(sourceName)
    .AddConsoleExporter();
if (!string.IsNullOrWhiteSpace(secrets.ApplicationInsightsConnectionString))
{
    tracerProviderBuilder.AddAzureMonitorTraceExporter(options => options.ConnectionString = secrets.ApplicationInsightsConnectionString);
}

using TracerProvider tracerProvider = tracerProviderBuilder.Build();

OpenAIClient client = ClientHelper.GetAzureOpenAIClient();

AIAgent agent = client
    .GetResponsesClient().AsIChatClient("gpt-6-luna")
    .AsAIAgent(
        name: "MyObservedAgent",
        instructions: "You are a Friendly AI Bot, answering questions")
    .AsBuilder()
    .UseOpenTelemetry(sourceName, options =>
    {
        options.EnableSensitiveData = true; //If the actual messages should be logged or not
    })
    .Build();

AgentSession session = await agent.CreateSessionAsync();

AgentResponse response1 = await agent.RunAsync("Hello, My name is Rasmus", session);
Utils.Red(response1.Text);
Utils.Separator();

AgentResponse response2 = await agent.RunAsync("What is the capital of France?", session);
Utils.Red(response2.Text);
Utils.Separator();

AgentResponse response3 = await agent.RunAsync("What was my name?", session);
Utils.Red(response3.Text);
Utils.Separator();