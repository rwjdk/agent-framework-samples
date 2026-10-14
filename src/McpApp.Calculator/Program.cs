using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Shared;

Utils.Init("McpApp.Calculator");

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<CalculatorTools>()
    .WithResources<CalculatorResources>();

WebApplication app = builder.Build();

app.MapMcp("/mcp");

app.Run();

[McpServerToolType]
public class CalculatorTools
{
    [McpServerTool(Name = "OpenCalculator"), Description("Opens a mini calculator. Supply first, operation (+, -, *, /), and second to calculate immediately and display the result.")]
    [McpMeta("ui", JsonValue = """{"resourceUri":"ui://calculator/view.html"}""")]
    [McpMeta("ui/resourceUri", "ui://calculator/view.html")]
    public static CallToolResult OpenCalculator(decimal first = 0, string operation = "+", decimal second = 0)
    {
        return Calculate(first, operation, second);
    }

    [McpServerTool(Name = "Calculate"), Description("Calculates two numbers using +, -, *, or / and displays the result in the calculator app.")]
    [McpMeta("ui", JsonValue = """{"resourceUri":"ui://calculator/view.html"}""")]
    [McpMeta("ui/resourceUri", "ui://calculator/view.html")]
    public static CallToolResult Calculate(
        [Description("The first number.")] decimal first,
        [Description("One of +, -, *, or /.")] string operation,
        [Description("The second number.")] decimal second)
    {
        decimal result;
        try
        {
            result = operation switch
            {
                "+" => first + second,
                "-" => first - second,
                "*" => first * second,
                "/" when second != 0 => first / second,
                "/" => throw new McpException("Cannot divide by zero."),
                _ => throw new McpException("Operation must be +, -, *, or /.")
            };
        }
        catch (OverflowException)
        {
            throw new McpException("The result is too large.");
        }

        return new CallToolResult
        {
            Content = [new TextContentBlock { Text = $"{first.ToString(CultureInfo.InvariantCulture)} {operation} {second.ToString(CultureInfo.InvariantCulture)} = {result.ToString(CultureInfo.InvariantCulture)}" }],
            StructuredContent = JsonSerializer.SerializeToElement(new { first, operation, second, result })
        };
    }
}

[McpServerResourceType]
public class CalculatorResources
{
    [McpServerResource(UriTemplate = "ui://calculator/view.html", Name = "Calculator", MimeType = "text/html;profile=mcp-app")]
    [Description("A simple calculator that follows the host's theme.")]
    public static Task<string> Calculator(CancellationToken cancellationToken)
    {
        return File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "calculator.html"), cancellationToken);
    }
}
