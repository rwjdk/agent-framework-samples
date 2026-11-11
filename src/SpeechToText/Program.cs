#pragma warning disable OPENAI001
using OpenAI.Responses;
using Microsoft.Extensions.AI;

using NAudio.Wave;
using OpenAI.Audio;
using Shared;
using System.ClientModel;
using Microsoft.Agents.AI;
using OpenAI;
using OpenAI.Chat;

Console.Clear();
Secrets secrets = SecretsManager.GetSecrets();

OpenAIClient client = ClientHelper.GetAzureOpenAIClient();
AudioClient audioClient = client.GetAudioClient("whisper");

ChatClientAgent agent = client
    .GetResponsesClient().AsIChatClient("gpt-6-luna")
    .AsAIAgent(instructions: "You are a Friendly AI Bot, answering questions");

AgentSession agentSession = await agent.CreateSessionAsync();

while (true)
{
    Console.WriteLine("Press any key to start recording...");
    Console.ReadKey();

    //Record the Audio
    using MemoryStream audioStream = RecordAudio();

    //Turn Audio into Text
    ClientResult<AudioTranscription> result = await audioClient.TranscribeAudioAsync(audioStream, "audio.wav");

    string questionFromAudio = result.Value.Text;
    Console.WriteLine($"> {questionFromAudio}");

    AgentResponse response = await agent.RunAsync(questionFromAudio, agentSession);
    Console.WriteLine(response);

    Utils.Separator();
}

MemoryStream RecordAudio()
{
    MemoryStream stream = new();
    using WaveIn waveIn = new();
    waveIn.WaveFormat = new WaveFormat(16000, 16, 1);
    using (WaveFileWriter writer = new(stream, waveIn.WaveFormat))
    {
        waveIn.DataAvailable += (_, args) => { writer.Write(args.Buffer, 0, args.BytesRecorded); };
        waveIn.StartRecording();

        Console.WriteLine("Recording... Press any key to stop");
        Console.ReadKey();

        waveIn.StopRecording();
    }
    stream.Position = 0;
    return stream;
}
