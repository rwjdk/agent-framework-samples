using AgentFrameworkToolkit.AzureOpenAI;
using AgentFrameworkToolkit.EasierTools.After;
using AgentFrameworkToolkit.EasierTools.Before;
using AgentFrameworkToolkit.OpenAI;
using AgentFrameworkToolkit.Tools;
using AgentFrameworkToolkit.Tools.ModelContextProtocol;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Shared;

Console.Clear();
Secrets secrets = SecretsManager.GetSecrets();

IList<AITool> tools = [];

#region Before

FileSystemToolsBefore toolsBefore = new FileSystemToolsBefore();
tools =
[
    AIFunctionFactory.Create(toolsBefore.GetRootFolder, "get_root_folder", "Get the root folder to work in"),
    AIFunctionFactory.Create(toolsBefore.CreateFolder, "create_folder", "Create a new folder"),
    AIFunctionFactory.Create(toolsBefore.CreateFile, "create_file", "Create a new file"),
    AIFunctionFactory.Create(toolsBefore.GetContentOfFile, "get_content_of_file", "Get the content of a file"),
    AIFunctionFactory.Create(toolsBefore.MoveFile, "move_file", "Move a File"),
    AIFunctionFactory.Create(toolsBefore.MoveFolder, "move_folder", "Move a Folder"),
    AIFunctionFactory.Create(toolsBefore.GetFiles, "get_files", "Get Files in a Folder"),
    AIFunctionFactory.Create(toolsBefore.GetFolders, "get_folders", "Get SubFolders in a Folder"),
    AIFunctionFactory.Create(toolsBefore.DeleteFolder, "delete_folder", "Delete a Folder"),
    AIFunctionFactory.Create(toolsBefore.DeleteFile, "delete_file", "Delete a File"),
];
//You could use some reflection but then names and descriptions are more difficult

#endregion

#region After

AIToolsFactory aiToolsFactory = new(); //Dependency Injection version: builder.Services.AddAIToolsFactory();

tools = aiToolsFactory.GetTools(typeof(FileSystemToolsAfter));

//or

FileSystemToolsAfter instance = new();
tools = aiToolsFactory.GetTools(instance); //If you do not have an empty constructor

//or from MCP tools

//Either remote MCP Server
McpClientTools toolsFromRemoteMcp = await aiToolsFactory.GetToolsFromRemoteMcpAsync("http://mcp.relewise.com");

//Or local MCP Server
McpClientTools toolsFromLocalMcp = await aiToolsFactory.GetToolsFromLocalMcpAsync("npx", ["@playwright/mcp@latest"]);

#endregion


AzureOpenAIAgentFactory agentFactory = new(new AzureOpenAIConnection
{
    DefaultClientType = ClientType.ResponsesApi,
    Endpoint = secrets.AzureOpenAiEndpoint,
    ApiKey = secrets.AzureOpenAiKey,
    NetworkTimeout = TimeSpan.FromMinutes(5)
});

AzureOpenAIAgent agent = agentFactory.CreateAgent(new AgentOptions
{
    Model = OpenAIChatModels.Gpt6Luna,
    Instructions = "You are a File Expert. When working with files you need to provide the full path; not just the filename",
    ReasoningEffort = OpenAIReasoningEffort.Low,
    Tools = tools,
    RawToolCallDetails = details => { Utils.Gray(details.ToString()); }
});

AgentResponse response = await agent.RunAsync("Create 10 files with random Fruit-names");
Console.WriteLine(response);