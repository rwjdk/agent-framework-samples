#pragma warning disable OPENAI001
using OpenAI.Responses;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using Shared;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text.Json;
using OpenAI;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

Utils.Init("AIContextProviders");
(Uri endpoint, ApiKeyCredential apiKey) = SecretsManager.GetAzureOpenAICredentials(false);

OpenAIClient client = ClientHelper.GetAzureOpenAIClient(true);

AIAgent agent = client
    .GetResponsesClient().AsIChatClient("gpt-6-luna")
    .AsAIAgent(new ChatClientAgentOptions
    {
        ChatOptions = new ChatOptions
        {
            Instructions = "Prefix all messages with 👋👋👋"
        },
        AIContextProviders = [
            new MyAIContextProvider(),
            new MyAIContextProvider2(),
            //new MyMessageAIContextProvider(),
        ]
    })
    .AsBuilder()
    .UseAIContextProviders([new MyMessageAIContextProvider()])
    .Build();

AgentSession session = await agent.CreateSessionAsync();

while (true)
{
    Console.Write("> ");
    string input = Console.ReadLine() ?? "";
    AgentResponse response = await agent.RunAsync(input, session);
    Utils.Separator();
    Utils.Red("Final Response:");
    Console.WriteLine(response);
    Utils.Separator();
}

class MyAIContextProvider : AIContextProvider
{
    /* Order of an AIContextProvider when an 'RunAsync' method is being executed
     *
     * - InvokingCoreAsync --> ProvideAIContextAsync
     * - LLM Call
     * - InvokedCoreAsync --> StoreAIContextAsync
     *
     */

    //Pre LLM Call (Core)
    protected override ValueTask<AIContext> InvokingCoreAsync(InvokingContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        //Only call this if you also want to be in charge of AIContext merging [You will most likely not want to override this]
        Utils.Gray("MyAIContextProvider.InvokingCoreAsync");
        return base.InvokingCoreAsync(context, cancellationToken);
    }

    //Pre LLM Call (Enrichment)
    protected override ValueTask<AIContext> ProvideAIContextAsync(InvokingContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        Utils.Gray("MyAIContextProvider.ProvideAIContextAsync");
        Utils.Gray("- Messages:");
        foreach (ChatMessage message in context.AIContext.Messages ?? [])
        {
            Utils.Gray($"-- {message.Role}: {message.Text}");
        }
        /* Use this to do the following:
         * - Inject additional instructions (for this one LLM Call)
         * - Inject additional tools (for this one LLM Call)
         * - Inject additional message (that unlike the two above will become part of chat-history)
         */

        return ValueTask.FromResult(new AIContext
        {
            Instructions = "Speak like a pirate",
            Tools = [],
        });
    }

    //Post LLM Call (Core)
    protected override ValueTask InvokedCoreAsync(InvokedContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        //Only call this if you also want to be in charge of Error Handling [You will most likely not want to override this]
        Utils.Gray("MyAIContextProvider.InvokedCoreAsync");
        return base.InvokedCoreAsync(context, cancellationToken);
    }

    //Post LLM Call (Leverage LLM Call Result for something)
    protected override async ValueTask StoreAIContextAsync(InvokedContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        Utils.Gray("MyAIContextProvider.StoreAIContextAsync");
        Utils.Gray($"- Exception? {context.InvokeException}");
        Utils.Gray("- Request Messages");
        foreach (ChatMessage message in context.RequestMessages ?? [])
        {
            Utils.Gray($"-- {message.Role}: {message.Text}");
        }
        Utils.Gray("- Response Messages");
        foreach (ChatMessage message in context.ResponseMessages ?? [])
        {
            Utils.Gray($"-- {message.Role}: {message.Text}");
        }
        /* Use this to do the following:
         * - Extract information out of the response in a centralized structured manner (aka example storing memory)
         * - Deal with exceptions
         */

        await Task.CompletedTask;
    }
}

class MyAIContextProvider2 : AIContextProvider
{
    //Pre LLM Call (Core)
    protected override ValueTask<AIContext> InvokingCoreAsync(InvokingContext context, CancellationToken cancellationToken = default)
    {
        //Only call this if you also want to be in charge of AIContext merging [You will most likely not want to override this]
        Utils.Gray("MyAIContextProvider2.InvokingCoreAsync");
        return base.InvokingCoreAsync(context, cancellationToken);
    }

    //Pre LLM Call (Enrichment)
    protected override ValueTask<AIContext> ProvideAIContextAsync(InvokingContext context, CancellationToken cancellationToken = default)
    {
        Utils.Gray("MyAIContextProvider2.ProvideAIContextAsync");
        Utils.Gray("- Messages:");
        foreach (ChatMessage message in context.AIContext.Messages ?? [])
        {
            Utils.Gray($"-- {message.Role}: {message.Text}");
        }
        return ValueTask.FromResult(new AIContext
        {
            Instructions = "Suffix response with: '🤖🤖🤖'",
            Tools = []
        });
    }

    //Post LLM Call (Core)
    protected override ValueTask InvokedCoreAsync(InvokedContext context, CancellationToken cancellationToken = default)
    {
        //Only call this if you also want to be in charge of Error Handling [You will most likely not want to override this]
        Utils.Gray("MyAIContextProvider2.InvokedCoreAsync");
        return base.InvokedCoreAsync(context, cancellationToken);
    }

    //Post LLM Call (Leverage LLM Call Result for something)
    protected override async ValueTask StoreAIContextAsync(InvokedContext context, CancellationToken cancellationToken = default)
    {
        Utils.Gray("MyAIContextProvider2.StoreAIContextAsync");
        Utils.Gray("- Request Messages");
        foreach (ChatMessage message in context.RequestMessages ?? [])
        {
            Utils.Gray($"-- {message.Role}: {message.Text}");
        }
        Utils.Gray("- Response Messages");
        foreach (ChatMessage message in context.ResponseMessages ?? [])
        {
            Utils.Gray($"-- {message.Role}: {message.Text}");
        }
        await Task.CompletedTask;
    }
}

class MyMessageAIContextProvider : MessageAIContextProvider
{
    /* Order of an MessageAIContextProvider when an 'RunAsync' method is being executed
     *
     * As Provider
     * - InvokingCoreAsync --> ProvideAIContextAsync --> ProvideMessagesAsync
     * - LLM Call
     * - InvokedCoreAsync --> StoreAIContextAsync
     *
     *------------------------------------------------------------------------------------
     *
     * As Middleware
     * - InvokingCoreAsync --> ProvideMessagesAsync
     * - LLM Call
     * - InvokedCoreAsync --> StoreAIContextAsync
     *
     */

    protected override ValueTask<IEnumerable<ChatMessage>> InvokingCoreAsync(InvokingContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        Utils.Gray("MyMessageAIContextProvider.InvokingCoreAsync (MessageAIContextProvider Edition)");
        //You will most likely not want to override this
        return base.InvokingCoreAsync(context, cancellationToken);
    }

    protected override ValueTask<IEnumerable<ChatMessage>> ProvideMessagesAsync(InvokingContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        Utils.Gray("MyMessageAIContextProvider.ProvideMessagesAsync");
        IList<ChatMessage> injectedMessages = [new(ChatRole.User, "Make all text UPPERCASE")];
        return ValueTask.FromResult<IEnumerable<ChatMessage>>(injectedMessages);
    }

    protected override ValueTask InvokedCoreAsync(InvokedContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        Utils.Gray("MyMessageAIContextProvider.InvokedCoreAsync");
        //You will most likely not want to override this
        return base.InvokedCoreAsync(context, cancellationToken);
    }

    protected override ValueTask StoreAIContextAsync(InvokedContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        Utils.Gray("MyMessageAIContextProvider.StoreAIContextAsync");
        return base.StoreAIContextAsync(context, cancellationToken);
    }

    //Not Called overrides

    protected override ValueTask<AIContext> InvokingCoreAsync(AIContextProvider.InvokingContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        Utils.Gray("MyMessageAIContextProvider.InvokingCoreAsync (AIContextProvider Edition)");
        //Notes: This is not called as Middleware in a 'MessageAIContextProvider' (not 100% sure if that is a bug)
        return base.InvokingCoreAsync(context, cancellationToken);
    }

    protected override ValueTask<AIContext> ProvideAIContextAsync(AIContextProvider.InvokingContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        Utils.Gray("MyMessageAIContextProvider.ProvideAIContextAsync");
        //Notes: This is not called as Middleware in a 'MessageAIContextProvider' (not 100% sure if that is a bug)
        return base.ProvideAIContextAsync(context, cancellationToken);
    }
}

class RawCallDetailsHttpHandler() : HttpClientHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string requestString = await request.Content?.ReadAsStringAsync(cancellationToken)!;
        Utils.Separator();
        Utils.Gray("LLM Call: "+MakePretty(requestString));
        Utils.Separator();
        HttpResponseMessage response = await base.SendAsync(request, cancellationToken);
        return response;

        static string MakePretty(string input)
        {
            try
            {
                JsonElement jsonElement = JsonSerializer.Deserialize<JsonElement>(input);
                return JsonSerializer.Serialize(jsonElement, new JsonSerializerOptions { WriteIndented = true });
            }
            catch
            {
                //Input is not JSON so treat as is
                return input;
            }
        }
    }
}