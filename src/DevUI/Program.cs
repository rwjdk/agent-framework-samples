#pragma warning disable OPENAI001
using OpenAI.Responses;
//YouTube video that cover this sample: https://youtu.be/y4k200pcMRY


using AgentFrameworkToolkit.Anthropic;
using AgentFrameworkToolkit.Google;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using Shared;
using System.ClientModel;

Secrets secrets = Shared.SecretsManager.GetSecrets();
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

OpenAIClient client = ClientHelper.GetAzureOpenAIClient();

// Register Services needed to run DevUI
builder.Services.AddChatClient(client.GetResponsesClient().AsIChatClient("gpt-6-luna")); //You need to register a chat client for the dummy agents to use
builder.Services.AddOpenAIResponses();
builder.Services.AddOpenAIConversations();

// Register "dummy" Agent
builder.AddAIAgent("Comic Book Guy", "You are comic-book guy from The Simpsons")
    .WithAITool(AIFunctionFactory.Create(GetWeather));

//Build a "normal" Agent
string realAgentName = "Real Agent";
AIAgent myAgent = client
    .GetResponsesClient().AsIChatClient("gpt-6-luna")
    .AsAIAgent(name: realAgentName, instructions: "Speak like a pirate", tools: [AIFunctionFactory.Create(GetWeather)]);

builder.AddAIAgent(realAgentName, (serviceProvider, key) => myAgent); //Get registered as a keyed singleton so name on real agent and key must match

if (!string.IsNullOrWhiteSpace(secrets.GoogleGeminiApiKey))
{
    //Build a Google Agent (Using AgentFrameworkToolkit)
    string agentName = "Google Agent";
    GoogleAgentFactory agentFactory = new(new GoogleConnection { ApiKey = secrets.GoogleGeminiApiKey });

    GoogleAgent googleAgent = agentFactory.CreateAgent(new GoogleAgentOptions
    {
        Name = agentName,
        Model = "gemini-2.5-flash"
    });
    builder.AddAIAgent(agentName, (serviceProvider, key) => googleAgent);
}

if (!string.IsNullOrWhiteSpace(secrets.AnthropicApiKey))
{
    //Build a Claude Agent (Using AgentFrameworkToolkit)
    string agentName = "Anthropic Agent";
    AnthropicAgentFactory agentFactory = new(new AnthropicConnection { ApiKey = secrets.AnthropicApiKey });

    AnthropicAgent anthropicAgent = agentFactory.CreateAgent(new AnthropicAgentOptions
    {
        Name = agentName,
        Model = AnthropicChatModels.ClaudeHaiku45,
        MaxOutputTokens = 1000
    });

    builder.AddAIAgent(agentName, (serviceProvider, key) => anthropicAgent);
}

// Register sample workflows
IHostedAgentBuilder frenchTranslator = builder.AddAIAgent("french-translator", "Translate any text you get into French");
IHostedAgentBuilder germanTranslator = builder.AddAIAgent("german-translator", "Translate any text you get into German");

builder.AddWorkflow("translation-workflow-sequential", (sp, key) =>
{
    IEnumerable<AIAgent> agentsForWorkflow = new List<IHostedAgentBuilder>() { frenchTranslator, germanTranslator }.Select(ab => sp.GetRequiredKeyedService<AIAgent>(ab.Name));
    return AgentWorkflowBuilder.BuildSequential(workflowName: key, agents: agentsForWorkflow);
}).AddAsAIAgent();

builder.AddWorkflow("translation-workflow-concurrent", (sp, key) =>
{
    IEnumerable<AIAgent> agentsForWorkflow = new List<IHostedAgentBuilder>() { frenchTranslator, germanTranslator }.Select(ab => sp.GetRequiredKeyedService<AIAgent>(ab.Name));
    return AgentWorkflowBuilder.BuildConcurrent(workflowName: key, agents: agentsForWorkflow);
}).AddAsAIAgent();


WebApplication app = builder.Build();

if (builder.Environment.IsDevelopment())
{
    //Needed for DevUI to function
    app.MapOpenAIResponses();
    app.MapOpenAIConversations();
    app.MapDevUI();
}

app.Run();

static string GetWeather(string city)
{
    return "It is sunny and 19 degrees";
}