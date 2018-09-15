using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Xml.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public delegate void ReceiveHandler(string val);

// to get supported languages make a get request to
// Accept: application/json
// https://dev.microsofttranslator.com/languages?api-version=1.0&scope=speech

// open a connection to...
// wss://dev.microsofttranslator.com/speech/translate

// Example:
// GET wss://dev.microsofttranslator.com/speech/translate?from=en-US&to=it-IT&features=texttospeech&voice=it-IT-Elsa&api-version=1.0
// Ocp-Apim-Subscription-Key: {subscription key}
// X-ClientTraceId: {GUID}

// Once the connection is established, the client begins streaming audio to the service.The client sends audio in chunks.
// Each chunk is transmitted using a Websocket message of type Binary.
// Audio input is in the Waveform Audio File Format(WAVE, or more commonly known as WAV due to its filename extension). 
// The client application should stream single channel, signed 16bit PCM audio sampled at 16 kHz.
// The first set of bytes streamed by the client will include the WAV header.
// A 44-byte header for a single channel signed 16 bit PCM stream sampled at 16 kHz is:
public class AudioTranslator : MonoBehaviour
{
    public static AudioTranslator instance;
        
    // Public fields accessible in the Unity Editor
    public string apiKey = "955f6747399a477e8f25c0fc52a2a1d6";

    private string translationTokenEndpoint = "https://api.cognitive.microsoft.com/sts/v1.0/issueToken";

    private ClientWebSocket client;
    private bool ready = false;

    public event ReceiveHandler Received;

    async void Start()
    {
        /*
        string from = "en-US";
        string to = "it-IT";
        string voice = "it-IT-Elsa";
        var uri = $"wss://dev.microsofttranslator.com/speech/translate?from={from}&to={to}&features=partial,texttospeech&voice={voice}&api-version=1.0";
        */
        string from = "en-US";
        string to = "it-IT";
        string features = "texttospeech";
        string voice = "it-IT-Elsa";
        string api = "1.0";
        string host = "wss://dev.microsofttranslator.com";
        string path = "/speech/translate";
        string uri = host + path +
            "?from=" + from +
            "&to=" + to +
            "&api-version=" + api +
            "&features=" + features +
            "&voice=" + voice;

        Debug.Log("starting web socket");
        client = new ClientWebSocket();
        client.Options.SetRequestHeader("Ocp-Apim-Subscription-Key", apiKey);

        await client.ConnectAsync(new Uri(uri), CancellationToken.None);
        Debug.Log("connected to socket");

        var wavHeader = new byte[44] {82, 73, 70, 70, 0, 0, 0, 0, 87, 65, 86, 69, 0, 102, 109, 116, 0, 0, 0, 16, 0, 1, 0, 1, 0, 0, 62, 128, 0, 0, 125, 0, 0, 2, 0, 16, 100, 97, 116, 97, 0, 0, 0, 0};
        var wavHeaderBuffer = new ArraySegment<byte>(wavHeader);
        send(wavHeaderBuffer);
        ready = true;

        Task.WhenAll(receiveData()).Wait();
    }

    private async void send(ArraySegment<byte> buff)
    {
        await client.SendAsync(buff, WebSocketMessageType.Binary, true, CancellationToken.None);
    }

    private async Task receiveData()
    {
        var inbuf = new byte[102400];
        var segment = new ArraySegment<byte>(inbuf);

        Console.WriteLine("Awaiting response.");
        while (client.State == WebSocketState.Open)
        {
            var result = await client.ReceiveAsync(segment, CancellationToken.None);
            switch (result.MessageType)
            {
                case WebSocketMessageType.Close:
                    Console.WriteLine("Received close message. Status: " + result.CloseStatus + ". Description: " + result.CloseStatusDescription);
                    await client.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                    break;
                case WebSocketMessageType.Text:
                    Console.WriteLine("Received text.");
                    Console.WriteLine(Encoding.UTF8.GetString(inbuf).TrimEnd('\0'));
                    break;
                case WebSocketMessageType.Binary:
                    Console.WriteLine("Received binary data: " + result.Count + " bytes.");
                    break;
            }
        }
    }
        
    public async Task WriteData(MemoryStream stream, int count)
    {
        if (!ready)
        {
            return;
        }
        ArraySegment<byte> buffer;
        if (stream.TryGetBuffer(out buffer))
        {
            send(buffer);
        }
    }

}
