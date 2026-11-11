#pragma warning disable OPENAI001
using OpenAI.Responses;

using AgentUserInteraction.Advanced.Server.AgUiSpecializedAgents;
using AgentUserInteraction.Advanced.SharedModels;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using Shared;
using System.ClientModel;

Secrets secrets = SecretsManager.GetSecrets();
OpenAIClient client = ClientHelper.GetAzureOpenAIClient();

string model = "gpt-6-luna";

ChatClientAgent clientToolAgent = client
    .GetResponsesClient().AsIChatClient(model)
    .AsAIAgent(instructions: "You are a nice AI");

ChatClientAgent weatherAgent = client
    .GetResponsesClient().AsIChatClient(model)
    .AsAIAgent(instructions: "You Report the Weather.",
        tools: [AIFunctionFactory.Create(GetWeather, name: "get_weather")]);

ChatClientAgent weatherAgentStructured = client
    .GetResponsesClient().AsIChatClient(model)
    .AsAIAgent(instructions: "Always call tool 'get_weather' first. Then tell a story about the city but leave out the weather details",
        tools: [AIFunctionFactory.Create(GetWeather, name: "get_weather")]);

ChatClientAgent movieStructuredOutputAgent = client
    .GetResponsesClient().AsIChatClient(model)
    .AsAIAgent(instructions: "You are a movie Expert");


WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddAGUIServer();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Cors",
        policy =>
        {
            policy.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});


WebApplication app = builder.Build();

app.MapAGUIServer("/clientToolAgent", clientToolAgent);
app.MapAGUIServer("/weatherAgent", weatherAgent);
app.MapAGUIServer("/weatherAgentWithStructuredContent", new AgUiStructuredToolsOutputAgent(weatherAgentStructured, "get_weather"));
app.MapAGUIServer("/movieAgent", new AgUiStructuredOutputAgent<MovieResult>(movieStructuredOutputAgent));

app.UseHttpsRedirection();
app.UseCors("Cors");
app.Run();

//Server-Tool
static WeatherReport GetWeather(string city)
{
    return new WeatherReport
    {
        City = city,
        Condition = "Sunny",
        DegreesC = 25,
        DegreesF = 77
    };
}
