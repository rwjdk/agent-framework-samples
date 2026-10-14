using RemoteMcpServer.Resources;
using Shared;

Utils.Init("MCP Resources");

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<SampleTools>()
    .WithResources<SampleResources>();

WebApplication app = builder.Build();

app.MapMcp("/mcp");

app.Run();