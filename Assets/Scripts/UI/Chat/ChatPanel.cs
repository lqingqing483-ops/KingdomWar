using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KingdomWar.AI;
namespace KingdomWar.UI
{
public class ChatPanel : basePanel
{
    [Header("UI组件")]
    [SerializeField] private Transform messageContainer;
    [SerializeField] private GameObject messageItemPrefab;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button sendButton;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private TextMeshProUGUI statusText;

    private AIChatService chatService;
    private GameObject currentAIMessage;
    private TextMeshProUGUI currentAIText;
    private bool isWaiting;

    protected override void Start()
    {
        base.Start();
        
        chatService = AIChatService.Instance;
        
        if (chatService == null)
        {
            var go = new GameObject("AIChatService");
            chatService = go.AddComponent<AIChatService>();
        }
        
        chatService.OnMessageStart += OnAIResponseStart;
        chatService.OnMessageReceived += OnAIResponseChunk;
        chatService.OnMessageComplete += OnAIResponseComplete;
        chatService.OnError += OnError;
        
        sendButton.onClick.AddListener(OnSendClicked);
        inputField.onSubmit.AddListener(OnInputSubmit);
        
        statusText.text = "";
        
        AddMessage("Hello! I am the AI assistant for KingdomWar. How can I help you?", false);
    }

    private void OnSendClicked()
    {
        SendMessage();
    }

    private void OnInputSubmit(string text)
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            SendMessage();
        }
    }

    private void SendMessage()
    {
        if (isWaiting || string.IsNullOrWhiteSpace(inputField.text))
            return;

        string message = inputField.text.Trim();
        inputField.text = "";
        inputField.ActivateInputField();
        
        AddMessage(message, true);
        
        isWaiting = true;
        sendButton.interactable = false;
        statusText.text = "AI is thinking...";
        
        chatService.SendMessage(message);
    }

    private void AddMessage(string content, bool isUser)
    {
        var item = Instantiate(messageItemPrefab, messageContainer);
        var texts = item.GetComponentsInChildren<TextMeshProUGUI>();
        
        foreach (var text in texts)
        {
            if (text.name.Contains("Content"))
            {
                text.text = content;
                text.color = isUser ? new Color(0.2f, 0.4f, 0.8f) : Color.black;
            }
        }
        
        var images = item.GetComponentsInChildren<Image>();
        foreach (var img in images)
        {
            if (img.name.Contains("Background"))
            {
                img.color = isUser ? new Color(0.85f, 0.92f, 1f) : new Color(0.95f, 0.95f, 0.95f);
            }
        }
        
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0;
    }

    private void OnAIResponseStart()
    {
        currentAIMessage = Instantiate(messageItemPrefab, messageContainer);
        var texts = currentAIMessage.GetComponentsInChildren<TextMeshProUGUI>();
        foreach (var text in texts)
        {
            if (text.name.Contains("Content"))
            {
                currentAIText = text;
                currentAIText.text = "";
                currentAIText.color = Color.black;
            }
        }
        
        var images = currentAIMessage.GetComponentsInChildren<Image>();
        foreach (var img in images)
        {
            if (img.name.Contains("Background"))
            {
                img.color = new Color(0.95f, 0.95f, 0.95f);
            }
        }
        
        statusText.text = "";
    }

    private void OnAIResponseChunk(string chunk)
    {
        if (currentAIText != null)
        {
            currentAIText.text += chunk;
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0;
        }
    }

    private void OnAIResponseComplete()
    {
        isWaiting = false;
        sendButton.interactable = true;
        statusText.text = "";
        currentAIMessage = null;
        currentAIText = null;
    }

    private void OnError(string error)
    {
        isWaiting = false;
        sendButton.interactable = true;
        statusText.text = "";
        
        if (currentAIMessage != null)
        {
            Destroy(currentAIMessage);
            currentAIMessage = null;
            currentAIText = null;
        }
        
        AddMessage($"Sorry, an error occurred: {error}", false);
    }

    private void OnDestroy()
    {
        if (chatService != null)
        {
            chatService.OnMessageStart -= OnAIResponseStart;
            chatService.OnMessageReceived -= OnAIResponseChunk;
            chatService.OnMessageComplete -= OnAIResponseComplete;
            chatService.OnError -= OnError;
        }
    }
}

}
