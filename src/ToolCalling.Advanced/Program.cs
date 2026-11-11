#pragma warning disable OPENAI001
using OpenAI.Responses;
//YouTube video that cover this sample
//- Basic: https://youtu.be/gJTodKpv8Ik
//- Advanced: https://youtu.be/dCtojrK8bKk
//- MCP: https://youtu.be/Y5IKdt9vdJM

#pragma warning disable MEAI001

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using Shared;
using System.ClientModel;
using System.Reflection;
using System.Text;
using OpenAI.Chat;
using ToolCalling.Advanced.Tools;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

Secrets secrets = SecretsManager.GetSecrets();

OpenAIClient client = ClientHelper.GetAzureOpenAIClient();

//Get tools via reflection
FileSystemTools target = new();
MethodInfo[] methods = typeof(FileSystemTools).GetMethods(BindingFlags.Public | BindingFlags.Instance);
List<AITool> listOfTools = methods.Select(x => AIFunctionFactory.Create(x, target)).Cast<AITool>().ToList();

//Approval Tools
listOfTools.Add(new ApprovalRequiredAIFunction(AIFunctionFactory.Create(DangerousTools.SomethingDangerous)));

AIAgent agent = client
    .GetResponsesClient().AsIChatClient("gpt-6-luna")
    .AsAIAgent(
        instructions: "You are a File Expert. When working with files you need to provide the full path; not just the filename",
        tools: listOfTools
    )
    .AsBuilder()
    .Use(ToolCallingMiddleware) //Middleware
    .Build();

AgentSession session = await agent.CreateSessionAsync();

while (true)
{
    Console.Write("> ");
    string? input = Console.ReadLine();
    ChatMessage message = new(ChatRole.User, input);
    AgentResponse response = await agent.RunAsync(message, session);
    List<ToolApprovalRequestContent> userInputRequests = response.Messages.SelectMany(x=> x.Contents).OfType<ToolApprovalRequestContent>().ToList();
    while (userInputRequests.Count > 0)
    {
        List<ChatMessage> userInputResponses = userInputRequests
            .Select(functionApprovalRequest =>
            {
                FunctionCallContent toolCallContent = (FunctionCallContent)functionApprovalRequest.ToolCall;
                Console.WriteLine($"The agent would like to invoke the following function, please reply Y to approve: Name {toolCallContent.Name}");
                return new ChatMessage(ChatRole.User, [functionApprovalRequest.CreateResponse(Console.ReadLine()?.Equals("Y", StringComparison.OrdinalIgnoreCase) ?? false)]);
            })
            .ToList();

        response = await agent.RunAsync(userInputResponses, session);
        userInputRequests = response.Messages.SelectMany(x => x.Contents).OfType<ToolApprovalRequestContent>().ToList();
    }

    Console.WriteLine(response);

    Utils.Separator();
}

async ValueTask<object?> ToolCallingMiddleware(AIAgent callingAgent, FunctionInvocationContext context, Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next, CancellationToken cancellationToken)
{
    StringBuilder functionCallDetails = new();
    functionCallDetails.Append($"- Tool Call: '{context.Function.Name}'");
    if (context.Arguments.Count > 0)
    {
        functionCallDetails.Append($" (Args: {string.Join(",", context.Arguments.Select(x => $"[{x.Key} = {x.Value}]"))}");
    }

    Utils.Gray(functionCallDetails.ToString());

    return await next(context, cancellationToken);
}

//Alternative way to register the middleware to reside inside a class (if you need extra dependencies)
/*
var services = new ServiceCollection()
   .AddSingleton<MyFilterClass>()
   .BuildServiceProvider();

AIAgent agent = client
    .GetResponsesClient().AsIChatClient(configuration.ChatDeploymentName)
    .AsAIAgent(
        instructions: "You are a File Expert. When working with files you need to provide the full path; not just the filename",
        tools: listOfTools
    )
    .AsBuilder()
    .Use(services.GetRequiredService<MyFilterClass>().FunctionCallMiddleware) //Middleware
    .Build();

public class MyFilterClass
   {
       private readonly ILogger<MyFilterClass> _logger;

       public MyFilterClass(ILoggerFactory loggerFactory)
       {
           this._logger = loggerFactory.CreateLogger<MyFilterClass>();
       }

       public async ValueTask<object?> FunctionCallMiddleware(AIAgent agent, FunctionInvocationContext context, Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next, CancellationToken cancellationToken)
       {
           this._logger.LogInformation($"Function Name: {context!.Function.Name} - Middleware 1 Pre-Invoke");
           var result = await next(context, cancellationToken);
           this._logger.LogInformation($"Function Name: {context!.Function.Name} - Middleware 1 Post-Invoke");

           return result;
       }
   }
*/