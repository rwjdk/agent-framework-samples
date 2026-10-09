# Parallel Search MCP

A Microsoft Agent Framework agent with the `web_search` and `web_fetch` tools
from [Parallel Search MCP](https://docs.parallel.ai/integrations/mcp/search-mcp).
It connects to `https://search.parallel.ai/mcp` using Streamable HTTP without a
Parallel API key. Anonymous use has lower rate limits.

From the repository root, install with the .NET 10 SDK:

```sh
dotnet restore src/Toolcalling.ParallelSearch
```

Check discovery, search and page extraction without model credentials:

```sh
dotnet run --project src/Toolcalling.ParallelSearch -- --check
```

To run the interactive agent, configure your OpenAI model credentials (model
inference uses your OpenAI account):

```sh
export OPENAI_API_KEY="your-openai-key"
export OPENAI_MODEL="gpt-4.1" # optional; this is the default
dotnet run --project src/Toolcalling.ParallelSearch
```

Try: `Search for the Microsoft Agent Framework documentation, read its overview,
and explain what it supports. Include source URLs.`

Type `exit` or send end-of-input to quit. Ctrl+C cancels active MCP and agent calls.
The `--check` mode invokes discovered MCP functions directly; it does not run a model.
This is a separate sample and does not change the existing GitHub MCP sample.
