using System;
using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Chat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatManager : MonoBehaviour, IChatClientListener
{
    private ChatClient m_client;
    protected internal ChatAppSettings chatAppSettings = new ChatAppSettings();

    [SerializeField] private TextMeshProUGUI m_text;
    private string m_channelToSubscribe;

    [SerializeField] private GameObject m_chatCanvas;

    public void DebugReturn(DebugLevel level, string message)
    {
        switch (level)
        {
            case DebugLevel.OFF:
                break;
            case DebugLevel.ERROR:
                Debug.LogError(message);
                break;
            case DebugLevel.WARNING:
                Debug.LogWarning(message);
                break;
            case DebugLevel.INFO:
                Debug.Log(message);
                break;
            case DebugLevel.ALL:
                Debug.Log(message);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(level), level, null);
        }
    }

    public void OnDisconnected()
    {
        m_client.Unsubscribe(new []{m_channelToSubscribe});
        m_chatCanvas.SetActive(false);
    }

    public void OnConnected()
    {
        m_chatCanvas.SetActive(true);
        m_client.Subscribe(m_channelToSubscribe);
    }

    public void OnChatStateChange(ChatState state)
    {
    }

    public void OnGetMessages(string channelName, string[] senders, object[] messages)
    {
        ShowChannel(m_channelToSubscribe);
    }

    public void OnPrivateMessage(string sender, object message, string channelName)
    {
        throw new System.NotImplementedException();
    }

    public void OnSubscribed(string[] channels, bool[] results)
    {
        ShowChannel(m_channelToSubscribe);
    }

    public void OnUnsubscribed(string[] channels)
    {
        throw new System.NotImplementedException();
    }

    public void OnStatusUpdate(string user, int status, bool gotMessage, object message)
    {
        throw new System.NotImplementedException();
    }

    public void OnUserSubscribed(string channel, string user)
    {
    }

    public void OnUserUnsubscribed(string channel, string user)
    {
    }
    
    public void StartChat(string nickName, string roomName)
    {
        // Obviously, this shouldn't be hard coded, but for the tests purpose, we'll live with it.
        // Photon is supposed to handle it in autonomy, but either I did not import it correctly or Fusion doesn't
        // include Photon Chat yet
        this.chatAppSettings.AppIdChat = "8804c77f-ef28-47eb-97f8-47440950c269";
        
        m_client = new ChatClient(this);
        m_client.ChatRegion = "EU";
        m_client.Connect(chatAppSettings.AppIdChat, "1.0", new AuthenticationValues(nickName));
        m_channelToSubscribe = roomName;
    }

    private void Update()
    {
        m_client?.Service();
    }

    public void SendMessageOnSubscribedChannel(string message)
    {
        if (!string.IsNullOrEmpty(message))
            m_client.PublishMessage(m_channelToSubscribe, message);
    }

    public void ShowChannel(string channelName)
    {
        if (string.IsNullOrEmpty(channelName))
        {
            return;
        }

        ChatChannel channel = null;
        bool found = m_client.TryGetChannel(channelName, out channel);
        if (!found)
        {
            Debug.LogError("ShowChannel failed to find channel: " + channelName);
            return;
        }

        m_text.text = channel.ToStringMessages();
    }

    
}
