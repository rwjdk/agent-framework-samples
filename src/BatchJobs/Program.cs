using OpenAI.Batch;
using OpenAI.Files;
using Shared;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

using OpenAI;

#pragma warning disable OPENAI001

Console.Clear();

Secrets secrets = SecretsManager.GetSecrets();

OpenAIClient client = new(secrets.OpenAiApiKey);
//OpenAIClient client = ClientHelper.GetAzureOpenAIClient();
OpenAIFileClient fileClient = client.GetOpenAIFileClient();
BatchClient batchClient = client.GetBatchClient();

OpenAIFile file = await fileClient.UploadFileAsync("requestData.jsonl", new FileUploadPurpose("batch"));
//OpenAIFile file = await fileClient.UploadFileAsync("requestData-azure.jsonl", new FileUploadPurpose("batch"));

BinaryContent createBatchPayload = BinaryContent.CreateJson(new CreateBatchRequest
{
    InputFileId = file.Id,
    Endpoint = "/v1/responses",
    CompletionWindow = "24h",
});

CreateBatchOperation batchOperation = await batchClient.CreateBatchAsync(createBatchPayload, waitUntilCompleted: false);

Utils.Yellow($"Batch ID: {batchOperation.BatchId}");

string status = string.Empty;
string[] terminationsStatuses = ["completed", "expired", "cancelled", "failed"];

string? outputFileId = null;
Stopwatch stopwatch = Stopwatch.StartNew();
while (!terminationsStatuses.Contains(status))
{
    await Task.Delay(TimeSpan.FromSeconds(10));
    ClientResult batchResult = await batchClient.GetBatchAsync(batchOperation.BatchId, new RequestOptions());
    string batchJson = batchResult.GetRawResponse().Content.ToString();
    BatchResponse response = JsonSerializer.Deserialize<BatchResponse>(batchJson)!;
    status = response.Status;
    Utils.Gray($"Elapsed time: {Convert.ToInt32(stopwatch.Elapsed.TotalSeconds)} sec: " +
                            $"Status: {response.Status} (Counts: Total={response.Counts.Total} | Completed: {response.Counts.Completed} | Failed: {response.Counts.Failed}) ");
    outputFileId = response.OutputFileId;
}

if (!string.IsNullOrWhiteSpace(outputFileId))
{
    ClientResult<BinaryData> download = await fileClient.DownloadFileAsync(outputFileId);
    string path = $"{Path.GetTempPath()}\\{outputFileId}.jsonl";
    await File.WriteAllBytesAsync(path, download.Value.ToArray());
    await Task.Factory.StartNew(() =>
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "notepad.exe",
            Arguments = path,
            UseShellExecute = true
        });
    });
}

class BatchResponse
{
    [JsonPropertyName("status")]
    public required string Status { get; init; } //Options: validating, failed, in_progress, finalizing, completed, expired, cancelling, cancelled

    [JsonPropertyName("request_counts")]
    public required BatchResponseCounts Counts { get; init; }

    [JsonPropertyName("output_file_id")]
    public required string? OutputFileId { get; init; }

    [JsonPropertyName("error_file_id")]
    public required string? ErrorFileId { get; init; }
}

class BatchResponseCounts
{
    [JsonPropertyName("total")]
    public required int Total { get; init; }

    [JsonPropertyName("completed")]
    public required int Completed { get; init; }

    [JsonPropertyName("failed")]
    public required int Failed { get; init; }
}

class CreateBatchRequest
{
    [JsonPropertyName("input_file_id")]
    public required string InputFileId { get; init; }

    [JsonPropertyName("endpoint")]
    public required string Endpoint { get; init; }

    [JsonPropertyName("completion_window")]
    public required string CompletionWindow { get; init; }
}