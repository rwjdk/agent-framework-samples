#pragma warning disable OPENAI001
using OpenAI.Responses;
using Microsoft.Extensions.AI;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AzureFunctions;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;
using OpenAI;
using OpenAI.Chat;
using Shared;
using System.ClientModel;

FunctionsApplicationBuilder builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

OpenAIClient client = ClientHelper.GetAzureOpenAIClient();
ChatClientAgent myAgent = client
    .GetResponsesClient().AsIChatClient("gpt-6-luna")
    .AsAIAgent(name: "MyAgent");

//From nuget: Microsoft.Agents.AI.Hosting.AzureFunctions
builder.ConfigureDurableAgents(options => options.AddAIAgent(myAgent));

builder.Build().Run();
