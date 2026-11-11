#pragma warning disable OPENAI001
using OpenAI.Responses;
//YouTube video that cover this sample: https://youtu.be/tDQc6lZUbYc


using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;
using OpenAI.Chat;
using Shared;
using System.ClientModel;

//Start with Business as Usual
Console.Clear();
Secrets secrets = SecretsManager.GetSecrets();
OpenAIClient client = ClientHelper.GetAzureOpenAIClient();

ChatClientAgent agent = client
    .GetResponsesClient().AsIChatClient("gpt-6-luna")
    .AsAIAgent(tools: [AIFunctionFactory.Create(GetWeather, name: "get_weather")]);

//AG-UI Part begin
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddAGUIServer();
WebApplication app = builder.Build();

app.MapAGUIServer("/", agent);

await app.RunAsync();

//Server-Tool
static string GetWeather(string city)
{
    return "It is sunny and 19 degrees";
}