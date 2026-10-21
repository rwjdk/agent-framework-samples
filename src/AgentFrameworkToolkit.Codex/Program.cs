using AgentFrameworkToolkit.Codex;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Shared;
#pragma warning disable AFT999

Console.WriteLine("Hello, World!");


Utils.Init("Codex in MAF - Full Demo");

//NB: You need to install and login to Codex CLI for this to work

CodexAgentFactory factory = new CodexAgentFactory(new CodexConnection
{
    //Various Advanced options (if you installed Codex in a special way) or use tokens instead 
});

string workPath = Path.Combine(Path.GetTempPath(), "My-Codex-Demo");
Directory.CreateDirectory(workPath);

CodexAgent agent = factory.CreateAgent(new CodexAgentOptions
{
    WorkingDirectory = workPath, //Choose any local folder... Will pick up Agents.md and Skills in that folder
    Model = "gpt-6.1-sol",
    ReasoningEffort = CodexReasoningEffort.Low,
    ServiceTier = CodexServiceTier.Normal,
    Permissions = CodexPermissions.WorkspaceWrite, //What type of access should Codex Have
    ApprovalPolicy = CodexApprovalPolicy.OnRequest, //Set to Never if you never want approval requests
    Instructions = "You are a nice AI", //Additional instructions beyond Agents.md file
    Tools = [AIFunctionFactory.Create(GetWeather)], //Pass your own C# driven tools to Codex for extra features
    OnUserInputRequested = HandleUserInputRequestedAsync,//Let Codex as Questions to the user
    OnRunEvent = HandleRunEvent,
    //+ Various Advanced settings to control Codex further
});

AgentSession session = await agent.CreateSessionAsync();

AgentResponse startConversation = await agent.RunAsync("Who is Elvis?", session);

string codexThreadId = session.GetThreadId()!;

AgentSession resumedSession = await agent.CreateSessionAsync(codexThreadId);

AgentResponse resumedConversation = await agent.RunAsync("How tall is he?", session);

//Let do a chat loop and test file generation, image generation etc

session = await agent.CreateSessionAsync();
while (true)
{
    Console.Write("> ");
    string? input = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(input))
    {
        AgentResponse response = await agent.RunAsync(input, session);
        while (response.Messages.SelectMany(message => message.Contents).OfType<ToolApprovalRequestContent>().ToList() is { Count: > 0 } approvals)
        {
            List<AIContent> decisions = [];
            foreach (ToolApprovalRequestContent approval in approvals)
            {
                FunctionCallContent call = (FunctionCallContent)approval.ToolCall;
                DisplayApproval(call);
                Utils.Yellow("Approve? [Y/N] ");
                decisions.Add(approval.CreateResponse(string.Equals(Console.ReadLine(), "y", StringComparison.OrdinalIgnoreCase)));
            }
            response = await agent.RunAsync(new ChatMessage(ChatRole.User, decisions), session);
        }
        Console.WriteLine(response);

        IReadOnlyList<CodexFileChange> fileChanges = response.GetFileChanges();
        foreach (CodexFileChange fileChange in fileChanges)
        {
            Console.WriteLine($"- File: {fileChange.Path}");
        }

        Console.WriteLine("---");
    }
}

static string GetWeather(string city)
{
    return "It is sunny and 19 Degrees";
}

static void DisplayApproval(FunctionCallContent call)
{
    Console.WriteLine();
    if (call.Name is "codex_command" or "codex_file_change")
    {
        Utils.Yellow(call.Name == "codex_command" ? "Permission to run a command" : "Permission to edit files");
        DisplayArgument("reason", "Reason");
        DisplayArgument("cwd", "Working directory");
        DisplayArgument("grantRoot", "Requested write directory");
        DisplayArgument("command", "Command");
    }
    else
    {
        Utils.Yellow($"Permission to call {call.Name}");
        if (call.Arguments is not null)
        {
            foreach (string name in call.Arguments.Keys)
            {
                DisplayArgument(name, name);
            }
        }
    }

    void DisplayArgument(string name, string label)
    {
        if (call.Arguments?.TryGetValue(name, out object? value) == true &&
            value?.ToString() is { } text && !string.IsNullOrWhiteSpace(text))
        {
            Utils.Yellow($"{label}: {text}");
        }
    }
}

static void HandleRunEvent(CodexRunEvent runEvent)
{
    // Completed snapshots contain the full text; displaying deltas too would repeat it.
    if (runEvent.Stage == CodexRunEventStage.Delta ||
        (runEvent.Kind == CodexRunEventKind.Message && runEvent.MessagePhase != "commentary"))
    {
        return;
    }

    Utils.Gray($"[{runEvent.Kind}] {runEvent.Summary}");
    if (!string.IsNullOrWhiteSpace(runEvent.Details))
    {
        Utils.Gray(runEvent.Details);
    }
    if (runEvent.Kind == CodexRunEventKind.Command && runEvent.Stage == CodexRunEventStage.Completed &&
        !string.IsNullOrWhiteSpace(runEvent.Text))
    {
        Utils.Gray(runEvent.Text.TrimEnd());
    }
    if (runEvent.Kind == CodexRunEventKind.ToolCall)
    {
        if (runEvent.Stage == CodexRunEventStage.Started && runEvent.ToolArguments is { } arguments)
        {
            Utils.Gray($"  Arguments: {arguments}");
        }
        if (runEvent.Stage == CodexRunEventStage.Completed)
        {
            if (!string.IsNullOrWhiteSpace(runEvent.Text) && !runEvent.Summary.Contains(runEvent.Text, StringComparison.Ordinal))
            {
                Utils.Gray(runEvent.Text);
            }
            else if (string.IsNullOrWhiteSpace(runEvent.Text) && runEvent.ToolResult is { } result)
            {
                Utils.Gray($"  Result: {result}");
            }
        }
    }
}

static Task<CodexUserInputResponse> HandleUserInputRequestedAsync(CodexUserInputRequest request, CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();
    Dictionary<string, string[]> answers = [];
    foreach (CodexUserInputQuestion question in request.Questions)
    {
        Utils.Green(question.Question);
        if (question.Options.Count > 0)
        {
            Utils.Green(string.Join(" / ", question.Options));
        }
        answers[question.Id] = [Console.ReadLine() ?? ""];
    }
    return Task.FromResult(request.AnswerQuestions(answers));
}