using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject m_networkManagerPrefab;
    
    public static GameMode s_gameMode;
    public static string s_nickName;
    public static string s_roomName;

    public static Dictionary<PlayerRef, Team> s_playerTeams = new();
    
    private NetworkManager m_networkManager;
    private ChatManager m_chatManager;

    public void SetGameMode(int mode)
    {
        s_gameMode = (GameMode)mode;
    }

    public void InitGame()
    {
        GameObject manager = Instantiate(m_networkManagerPrefab, this.transform);

        SceneManager.LoadScene(1);
        m_networkManager = manager.GetComponent<NetworkManager>();
        m_chatManager = manager.GetComponent<ChatManager>();
        
        m_networkManager.StartGame(s_gameMode, s_nickName, s_roomName);
        m_chatManager.StartChat(s_nickName, s_roomName);
    }
}
