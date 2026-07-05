using System;
using System.Collections.Generic;
namespace KingdomWar.AI
{
[Serializable]
public class ChatRequest
{
    public string model;
    public List<ChatMessage> messages;
    public float temperature;
    public int max_tokens;
    public bool stream;
}

[Serializable]
public class ChatMessage
{
    public string role;
    public string content;
    
    public ChatMessage(string role, string content)
    {
        this.role = role;
        this.content = content;
    }
}

[Serializable]
public class ChatResponse
{
    public string id;
    public string object_type;
    public List<ChatChoice> choices;
    public ChatUsage usage;
}

[Serializable]
public class ChatChoice
{
    public int index;
    public ChatMessage message;
    public ChatDelta delta;
    public string finish_reason;
}

[Serializable]
public class ChatDelta
{
    public string content;
    public string reasoning_content;
}

[Serializable]
public class ChatUsage
{
    public int prompt_tokens;
    public int completion_tokens;
    public int total_tokens;
}

[Serializable]
public class StreamChunk
{
    public string id;
    public string object_type;
    public List<StreamChoice> choices;
}

[Serializable]
public class StreamChoice
{
    public int index;
    public ChatDelta delta;
    public string finish_reason;
}

[Serializable]
internal class ErrorResponse
{
    public ErrorDetail error;
}

[Serializable]
internal class ErrorDetail
{
    public string message;
    public string type;
    public string code;
}

}
