using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
namespace KingdomWar.AI
{
public class AIChatService : MonoBehaviour
{
    public static AIChatService Instance { get; private set; }
    
    [SerializeField] private AIChatConfig config;
    
    private List<ChatMessage> conversationHistory = new List<ChatMessage>();
    
    public event Action<string> OnMessageReceived;
    public event Action<string> OnError;
    public event Action OnMessageStart;
    public event Action OnMessageComplete;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            if (config == null)
            {
                config = Resources.Load<AIChatConfig>("Config/AIChatConfig");
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetConfig(AIChatConfig newConfig)
    {
        config = newConfig;
    }

    public void SendMessage(string userMessage)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
            return;
        
        if (config == null)
        {
            OnError?.Invoke("Config file not loaded");
            return;
        }
        
        StartCoroutine(SendRequestCoroutine(userMessage));
    }

    private IEnumerator SendRequestCoroutine(string userMessage)
    {
        conversationHistory.Add(new ChatMessage("user", userMessage));
        
        var request = new ChatRequest
        {
            model = config.model,
            messages = BuildMessages(),
            temperature = config.temperature,
            max_tokens = config.maxTokens,
            stream = true
        };
        
        string json = JsonUtility.ToJson(request);
        byte[] body = Encoding.UTF8.GetBytes(json);
        
        string url = config.baseUrl + "/chat/completions";
        
        using (var webRequest = new UnityWebRequest(url, "POST"))
        {
            webRequest.uploadHandler = new UploadHandlerRaw(body);
            webRequest.downloadHandler = new StreamDownloadHandler(this);
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.SetRequestHeader("Authorization", $"Bearer {config.apiKey}");
            webRequest.timeout = 60;
            
            yield return webRequest.SendWebRequest();
            
            if (webRequest.result == UnityWebRequest.Result.ConnectionError)
            {
                OnError?.Invoke("Network connection failed, please check network");
            }
            else if (webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                string errorMsg = ParseErrorResponse(webRequest.downloadHandler.text);
                OnError?.Invoke(errorMsg);
            }
        }
    }

    private string ParseErrorResponse(string responseText)
    {
        try
        {
            var error = JsonUtility.FromJson<ErrorResponse>(responseText);
            return error.error?.message ?? "请求失败";
        }
        catch
        {
            return "请求失败";
        }
    }

    private List<ChatMessage> BuildMessages()
    {
        var messages = new List<ChatMessage>
        {
            new ChatMessage("system", config.systemPrompt)
        };
        messages.AddRange(conversationHistory);
        return messages;
    }

    public void ClearHistory()
    {
        conversationHistory.Clear();
    }

    internal void HandleStreamChunk(string content)
    {
        if (!string.IsNullOrEmpty(content))
        {
            OnMessageReceived?.Invoke(content);
        }
    }

    internal void HandleStreamStart()
    {
        OnMessageStart?.Invoke();
    }

    internal void HandleStreamEnd(string fullResponse)
    {
        conversationHistory.Add(new ChatMessage("assistant", fullResponse));
        OnMessageComplete?.Invoke();
    }
}

internal class StreamDownloadHandler : DownloadHandlerScript
{
    private AIChatService service;
    private StringBuilder responseBuilder = new StringBuilder();
    private StringBuilder buffer = new StringBuilder();
    private bool isFirstChunk = true;

    public StreamDownloadHandler(AIChatService service) : base()
    {
        this.service = service;
    }

    protected override bool ReceiveData(byte[] data, int dataLength)
    {
        if (data == null || dataLength == 0)
            return true;

        string text = Encoding.UTF8.GetString(data, 0, dataLength);
        buffer.Append(text);
        
        ProcessBuffer();
        
        return true;
    }

    private void ProcessBuffer()
    {
        string content = buffer.ToString();
        string[] lines = content.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
        
        buffer.Clear();
        
        foreach (string line in lines)
        {
            if (line.StartsWith("data: "))
            {
                string json = line.Substring(6).Trim();
                
                if (json == "[DONE]")
                {
                    service.HandleStreamEnd(responseBuilder.ToString());
                    continue;
                }
                
                try
                {
                    var chunk = JsonUtility.FromJson<StreamChunk>(json);
                    if (chunk?.choices != null && chunk.choices.Count > 0)
                    {
                        var delta = chunk.choices[0].delta;
                        if (delta?.content != null)
                        {
                            if (isFirstChunk)
                            {
                                isFirstChunk = false;
                                service.HandleStreamStart();
                            }
                            
                            responseBuilder.Append(delta.content);
                            service.HandleStreamChunk(delta.content);
                        }
                    }
                }
                catch
                {
                    buffer.AppendLine(line);
                }
            }
            else if (!string.IsNullOrWhiteSpace(line))
            {
                buffer.AppendLine(line);
            }
        }
    }
}

}
