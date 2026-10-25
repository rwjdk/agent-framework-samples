using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Playwright;
using OpenAI.Responses;
using BrowserAutomation.Playwright;
using Shared;
#pragma warning disable OPENAI001

Utils.Init("Browser Automation - Playwright");

using IPlaywright playwright = await Playwright.CreateAsync();
if (!File.Exists(playwright.Chromium.ExecutablePath))
{
    Console.WriteLine("Installing Chromium...");
    int exitCode = Microsoft.Playwright.Program.Main(["install", "chromium"]);
    if (exitCode != 0)
    {
        throw new InvalidOperationException($"Chromium installation failed with exit code {exitCode}.");
    }
}

await using IBrowser browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

await using IBrowserContext context = await browser.NewContextAsync(new()
{
    ViewportSize = new() { Width = 1024, Height = 768 },
});
IPage page = await context.NewPageAsync();
page.SetDefaultTimeout(5000);
string sampleHtml = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Sample.html"));
await page.SetContentAsync(sampleHtml);

BrowserTools tools = new(page);

AIAgent agent = ClientHelper.GetAzureOpenAIClient().GetResponsesClient().AsAIAgent(
    model: "gpt-6-luna",
    instructions: """
                  Use the browser tools to complete the user's task. 
                  Steps:
                  1. Read the page to discover controls
                  2. Use labels and button names you have observed
                  3. After submitting a form or search, call read_page to inspect the result.
                  
                  Rules:
                  - Report success only when supported by the page.
                  - If a terms or cookie consent dialog appears, accept it using the controls shown on the page.
                  - If a CAPTCHA or other blocker prevents completion, report it and stop.
                  """,
    tools:
    [
        AIFunctionFactory.Create(tools.ReadPage, "read_page"),
        AIFunctionFactory.Create(tools.Navigate, "navigate"),
        AIFunctionFactory.Create(tools.FillField, "fill_field"),
        AIFunctionFactory.Create(tools.ClickButton, "click_button"),
        AIFunctionFactory.Create(tools.PressKey, "press_key")
    ]);

string input = """
               Task 1: 
               - Fill the local newsletter form with name Ada Lovelace and email ada@example.com. 
               - Click Subscribe
               - Verify the confirmation shows both values. 
               
               Task 2 (begin once Task 1 is done):
               - Navigate to https://www.google.com. 
               - Find the search field, search for banana
               - Verify that the resulting page shows search results for banana. 
               
               Finishing: 
               - Report the outcome of both tasks.
               """;

AgentResponse response = await agent.RunAsync(input);
Console.WriteLine(response);

if (response.Usage is { } usage)
{
    Console.WriteLine($"Tokens spent: input {usage.InputTokenCount:N0}, " +
                      $"output {usage.OutputTokenCount:N0}, " +
                      $"total {usage.TotalTokenCount ?? usage.InputTokenCount + usage.OutputTokenCount:N0}");
}

Console.WriteLine("Press Enter to close the browser.");
Console.ReadLine();