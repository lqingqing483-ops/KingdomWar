using UnityEngine;
namespace KingdomWar.AI
{
[CreateAssetMenu(fileName = "AIChatConfig", menuName = "Config/AI Chat Config")]
public class AIChatConfig : ScriptableObject
{
    [Header("API设置")]
    [Tooltip("阿里百炼API Key")]
    public string apiKey = "";
    
    [Tooltip("API地址")]
    public string baseUrl = "https://dashscope.aliyuncs.com/compatible-mode/v1";
    
    [Tooltip("模型名称")]
    public string model = "qwen3.5-plus";
    
    [Header("参数设置")]
    [Range(0f, 2f)] 
    [Tooltip("温度参数，越高越随机")]
    public float temperature = 0.7f;
    
    [Range(50, 2000)] 
    [Tooltip("Max Output Tokens")]
    public int maxTokens = 500;
    
    [Header("System Prompt")]
    [TextArea(3, 10)] 
    [Tooltip("AI Role Setting")]
    public string systemPrompt = "You are the AI assistant for KingdomWar. Answer players concisely in Chinese, max 200 words per reply.";
}

}
