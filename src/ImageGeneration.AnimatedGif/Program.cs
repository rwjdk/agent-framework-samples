using System.ClientModel;
using System.Diagnostics;
using System.Text.Json;
using OpenAI;
using OpenAI.Images;
using Shared;

Utils.Init("Image Generation - Animated GIF");

const string model = "gpt-image-2.6-flare";
/* Models that support GIF: gpt-image-2.6-flare or gpt-image-2.6-sunburst.
 * **********************************
 * Prices as of 11th of November 2026
 * Text input:      $5 / million tokens
 * Image output:    $30 / million tokens
 */
const decimal textInputPricePerMillion = 5m;
const decimal imageOutputPricePerMillion = 30m;

string prompt =
    """
    Create a seamless looping animation of a single cute brown 3D cartoon monkey in emoji style.
    The monkey starts with its eyes visible, raises both hands to cover its eyes completely,
    pauses briefly, then lowers its hands back to the starting pose.
    Keep the monkey centered against a plain pale yellow background with a fixed camera.
    """;

int frameCount = 16;
int frameDurationMs = 200;

OpenAIClient client = ClientHelper.GetOpenAIClient();

ImageClient imageClient = client.GetImageClient(model);

using BinaryContent content = BinaryContent.Create(BinaryData.FromObjectAsJson(new 
{
    // JSON request body (as no overload in nuget support the gif generation so far).
    model, 
    prompt,
    output_format = "gif",
    frame_count = frameCount, // Frames: 2-16.
    frame_duration_ms = frameDurationMs, // 20-2000 ms, rounded to 10 ms.
    size = "auto", // auto | WxH (example 512x512);
    quality = "medium" // auto | low | medium | high | xhigh | max
}));


Utils.Gray($"Generating {frameCount} frames at {frameDurationMs} ms per frame...");

try
{
    ClientResult result = await imageClient.GenerateImagesAsync(content);
    using JsonDocument response = JsonDocument.Parse(result.GetRawResponse().Content.ToMemory());
    
    byte[] gif = response.RootElement.GetProperty("data")[0].GetProperty("b64_json").GetBytesFromBase64();
    
    string path = Path.Combine(Path.GetTempPath(), $"animation-{Guid.NewGuid():N}.gif");
    
    await File.WriteAllBytesAsync(path, gif);
    
    Process.Start(new ProcessStartInfo("msedge.exe", $"\"{path}\"") { UseShellExecute = true });

    Utils.LineSeparator();

    //Cost
    if (response.RootElement.TryGetProperty("usage", out JsonElement usage) && usage.ValueKind == JsonValueKind.Object)
    {
        long inputTokens = usage.GetProperty("input_tokens").GetInt64();
        long outputTokens = usage.GetProperty("output_tokens").GetInt64();
        decimal inputCost = inputTokens * textInputPricePerMillion / 1_000_000m;
        decimal outputCost = outputTokens * imageOutputPricePerMillion / 1_000_000m;

        Utils.Gray($"Input tokens: {inputTokens:N0}");
        Utils.Gray($"Output tokens: {outputTokens:N0}");
        Utils.Green($"Estimated cost: USD {inputCost + outputCost:F6}");
    }
}
catch (ClientResultException exception)
{
    Utils.Red($"Image generation failed (HTTP {exception.Status}): {exception.Message}");
}
