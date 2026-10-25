
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text.Json;
using OpenAI;

namespace Shared;

public static class ClientHelper
{
    public static OpenAIClient GetOpenAIClient(bool showRawCall = false, RawCallOptions? rawCall = null)
    {
        string apiKey = SecretsManager.GetOpenAICredentials();
        if (!showRawCall)
        {
            return new OpenAIClient(apiKey);
        }

        return new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions
        {
            Transport = new HttpClientPipelineTransport(new HttpClient(new CustomClientHttpHandler(rawCall ?? new RawCallOptions())))
        });
    }

    public static OpenAIClient GetAzureOpenAIClient(bool showRawCall = false, RawCallOptions? rawCall = null)
    {
        (Uri endpoint, ApiKeyCredential apiKey) = SecretsManager.GetAzureOpenAICredentials(true);
        if (!showRawCall)
        {
            return new OpenAIClient(apiKey, new OpenAIClientOptions
            {
                Endpoint = endpoint
            });
        }

        return new OpenAIClient(apiKey, new OpenAIClientOptions
        {
            Endpoint = endpoint,
            Transport = new HttpClientPipelineTransport(new HttpClient(new CustomClientHttpHandler(rawCall ?? new RawCallOptions())))
        });
    }

    public class RawCallOptions
    {
        public bool ShowUrl { get; set; }
        public bool ShowRequest { get; set; } = true;
        public bool ShowResponse { get; set; }
    }

    class CustomClientHttpHandler(RawCallOptions rawCallOptions) : HttpClientHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string requestString = await request.Content?.ReadAsStringAsync(cancellationToken)!;
            if (rawCallOptions.ShowUrl)
            {
                Utils.Green($"Raw Request ({request.RequestUri})");
            }

            if (rawCallOptions.ShowRequest)
            {
                Utils.Gray(MakePretty(requestString));
                Utils.Separator();
            }

            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

            if (rawCallOptions.ShowResponse)
            {
                string responseString = await response.Content.ReadAsStringAsync(cancellationToken);
                Utils.Green("Raw Response");
                Utils.Gray(MakePretty(responseString));
                Utils.Separator();
            }

            return response;
        }

        private string MakePretty(string input)
        {
            try
            {
                JsonElement jsonElement = JsonSerializer.Deserialize<JsonElement>(input);
                return JsonSerializer.Serialize(jsonElement, new JsonSerializerOptions { WriteIndented = true });
            }
            catch
            {
                return input;
            }
        }
    }

}