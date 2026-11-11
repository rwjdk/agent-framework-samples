using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DoItYourselfAiCalling.Models;

internal class MyAgent(HttpClient? client = null)
{
    private readonly HttpClient _httpClient = client ?? new HttpClient();
    public required Provider Provider { get; set; }
    public string? Instructions { get; set; }
    public JsonObject? ResponseFormat { get; set; }
    public IList<MyTool> Tools { get; set; } = [];

    private readonly List<MyMessage> _messagesBackFromLastResponse = [];

    public async Task<MyResponse> RunAsync(string message)
    {
        return await RunAsync(new List<MyMessage>
        {
            new()
            {
                Role = "user",
                Content = message
            }
        });
    }

    public async Task<MyResponse<T>> RunAsync<T>(string message)
    {
        return await RunAsync<T>(new List<MyMessage>
        {
            new()
            {
                Role = "user",
                Content = message
            }
        });
    }

    public async Task<MyResponse> RunAsync(IList<MyMessage> messages)
    {
        IList<MyMessage> messagesToSend = GetMessagesToSend(messages);

        JsonObject json = BuildPayload(messagesToSend);

        string jsonResponse = await CallProvider(json);

        while (true)
        {
            using JsonDocument response = JsonDocument.Parse(jsonResponse);
            JsonElement root = response.RootElement;
            if (root.GetProperty("status").GetString() != "completed")
            {
                throw new InvalidOperationException($"Response did not complete: {jsonResponse}");
            }

            JsonArray toolOutputs = [];
            foreach (JsonElement item in root.GetProperty("output").EnumerateArray())
            {
                string? type = item.GetProperty("type").GetString();
                if (type == "message")
                {
                    string text = string.Concat(item.GetProperty("content").EnumerateArray()
                        .Where(x => x.GetProperty("type").GetString() == "output_text")
                        .Select(x => x.GetProperty("text").GetString()));
                    _messagesBackFromLastResponse.Add(new MyMessage { Role = "assistant", Content = text });
                }
                else if (type == "function_call")
                {
                    string name = item.GetProperty("name").GetString()!;
                    string callId = item.GetProperty("call_id").GetString()!;
                    string arguments = item.GetProperty("arguments").GetString()!;
                    MyTool tool = Tools.FirstOrDefault(x => x.Name == name)
                        ?? throw new InvalidOperationException($"Unknown tool: {name}");
                    using JsonDocument args = JsonDocument.Parse(arguments);
                    object?[] values = tool.Delegate.Method.GetParameters()
                        .Select(p => JsonSerializer.Deserialize(args.RootElement.GetProperty(p.Name!).GetRawText(), p.ParameterType))
                        .ToArray();
                    object? result = tool.Delegate.DynamicInvoke(values);
                    if (result is Task task)
                    {
                        await task;
                        result = task.GetType().GetProperty("Result")?.GetValue(task);
                    }
                    string output = result?.ToString() ?? string.Empty;
                    _messagesBackFromLastResponse.Add(new MyMessage
                    {
                        Role = "assistant", Content = string.Empty,
                        ToolCalls = [new MyMessageToolCallRequest
                        {
                            Id = callId, Type = "function",
                            Function = new Function { Name = name, Arguments = arguments }
                        }]
                    });
                    _messagesBackFromLastResponse.Add(new MyMessage { Role = "tool", Content = output, ToolCallId = callId });
                    toolOutputs.Add(new JsonObject { ["type"] = "function_call_output", ["call_id"] = callId, ["output"] = output });
                }
            }
            if (toolOutputs.Count == 0)
            {
                MyResponse finalResponse = new(new List<MyMessage>(_messagesBackFromLastResponse));
                _messagesBackFromLastResponse.Clear();
                return finalResponse;
            }
            json = BuildPayload([]);
            json["input"] = toolOutputs;
            json["previous_response_id"] = root.GetProperty("id").GetString();
            jsonResponse = await CallProvider(json);
        }
    }

    public async Task<MyResponse<T>> RunAsync<T>(IList<MyMessage> messages)
    {
        ResponseFormat = BuildResponseFormat<T>();
        MyResponse response = await RunAsync(messages);
        ResponseFormat = null;
        return new MyResponse<T>(response);
    }

    private JsonObject BuildResponseFormat<T>()
    {
        Type type = typeof(T);

        Dictionary<string, object> properties = [];
        PropertyInfo[] propertyInfos = type.GetProperties();
        foreach (PropertyInfo propertyInfo in propertyInfos)
        {
            properties.Add(propertyInfo.Name, new JsonObject
            {
                {"type", CSharpTypeToSchemaType(propertyInfo.PropertyType)}
            });
        }

        JsonObject root = new()
        {
            {"type", "json_schema"},
            {"json_schema", new JsonObject
            {
                {"name", type.Name},
                {"schema", new JsonObject
                {
                    {"$schema", "https://json-schema.org/draft/2020-12/schema"},
                    {"type", "object"},
                    {"properties", JsonSerializer.SerializeToNode(properties)}
                }}
            } }
        };
        return root;
    }

    private string CSharpTypeToSchemaType(Type type)
    {
        if (type == typeof(String))
        {
            return "string";
        }
        if (type == typeof(Int32))
        {
            return "integer";
        }

        throw new Exception("Not yet supported type");
    }

    private async Task<string> CallProvider(JsonObject json)
    {
        string urlToUse = Provider.Endpoint.TrimEnd('/');
        string suffix = "/openai/v1";
        if (urlToUse.EndsWith(suffix))
        {
            //strip off the default mentioned in Azure Portal if given
            urlToUse = urlToUse.Remove(urlToUse.Length - suffix.Length);
        }
        urlToUse += "/openai/v1/responses";

        using HttpRequestMessage request = new(HttpMethod.Post, new Uri(urlToUse));
        request.Headers.Authorization = new("Bearer", Provider.ApiKey);
        request.Content = JsonContent.Create(json);
        using HttpResponseMessage httpResponse = await _httpClient.SendAsync(request);
        httpResponse.EnsureSuccessStatusCode();
        return await httpResponse.Content.ReadAsStringAsync();
    }

    private IList<MyMessage> GetMessagesToSend(IList<MyMessage> messages)
    {
        IList<MyMessage> messagesToSend = messages;
        if (!string.IsNullOrWhiteSpace(Instructions) && messagesToSend.All(x=> x.Role != "system"))
        {
            messagesToSend.Insert(0, new MyMessage
            {
                Role = "system",
                Content = Instructions
            }
            );
        }

        return messagesToSend;
    }

    private JsonObject BuildPayload(IList<MyMessage> messagesToSend)
    {
        //Message part of payload
        JsonArray jsonMessages = [];
        foreach (MyMessage message in messagesToSend)
        {
            if (message.Role == "tool")
            {
                jsonMessages.Add(new JsonObject { ["type"] = "function_call_output", ["call_id"] = message.ToolCallId, ["output"] = message.Content });
                continue;
            }
            if (message.ToolCalls?.Count > 0)
            {
                foreach (MyMessageToolCallRequest call in message.ToolCalls)
                {
                    jsonMessages.Add(new JsonObject { ["type"] = "function_call", ["call_id"] = call.Id, ["name"] = call.Function.Name, ["arguments"] = call.Function.Arguments });
                }
                continue;
            }
            JsonObject jsonMessage = new()
            {
                {"role", message.Role},

            };
            if (!string.IsNullOrWhiteSpace(message.Content))
            {
                jsonMessage.Add("content", message.Content);
            }

            jsonMessages.Add(jsonMessage);
        }

        JsonObject json = new()
        {
            {"model", Provider.Model},
            {"input", jsonMessages}
        };

        //Structured Output?
        if (ResponseFormat != null)
        {
            JsonObject format = (JsonObject)ResponseFormat["json_schema"]!.DeepClone();
            format["type"] = "json_schema";
            format["strict"] = true;
            JsonObject schema = (JsonObject)format["schema"]!;
            schema["additionalProperties"] = false;
            schema["required"] = new JsonArray(((JsonObject)schema["properties"]!).Select(x => (JsonNode?)JsonValue.Create(x.Key)).ToArray());
            json.Add("text", new JsonObject { ["format"] = format });
        }

        //Tools?
        if (Tools.Count > 0)
        {
            JsonArray tools = [];
            foreach (MyTool tool in Tools)
            {
                ParameterInfo[] parameterInfos = tool.Delegate.Method.GetParameters();
                Dictionary<string, object> parameters = [];
                foreach (ParameterInfo parameterInfo in parameterInfos)
                {
                    parameters.Add(parameterInfo.Name!, new JsonObject
                    {
                        {"type", CSharpTypeToSchemaType(parameterInfo.ParameterType)}
                    });
                }

                tools.Add(new JsonObject
                {
                    {"type", "function"},
                    {"strict", false},
                        {"description", tool.Description},
                        {"name", tool.Name},
                        {"parameters", new JsonObject
                        {
                            {"type", "object"},
                            {"properties", JsonSerializer.SerializeToNode(parameters)}
                        }}
                });
            }

            json.Add("tools", tools);
        }

        return json;
    }

}
