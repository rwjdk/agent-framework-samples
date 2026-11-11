#pragma warning disable OPENAI001
using OpenAI.Responses;
using Microsoft.Extensions.AI;
//YouTube video that cover this sample: https://youtu.be/q-mHdd6iJJo


using DependencyInjection.Components;
using Microsoft.Agents.AI;
using OpenAI;
using OpenAI.Chat;
using Shared;
using System.ClientModel;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

OpenAIClient client = ClientHelper.GetAzureOpenAIClient();
builder.Services.AddSingleton(client);

IChatClient chatClient = client.GetResponsesClient().AsIChatClient("gpt-6-luna");
builder.Services.AddKeyedSingleton("gpt-6-luna", chatClient);

ChatClientAgent agent = chatClient.AsAIAgent();
builder.Services.AddKeyedSingleton("gpt-6-luna", agent);

WebApplication app = builder.Build();
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.Run();