using UnityEngine;
using FlutterUnityIntegration;

public class UnityMessageSender : MonoBehaviour
{
    private UnityMessageManager messageManager;

    void Start()
    {
        // Get the UnityMessageManager component
        messageManager = GetComponent<UnityMessageManager>();
    }

    public void SendMessageToFlutter(string message)
    {
        if (messageManager != null)
        {
            messageManager.SendMessageToFlutter(message);
        }
        else
        {
            Debug.LogError("UnityMessageManager not found!");
        }
    }
}
