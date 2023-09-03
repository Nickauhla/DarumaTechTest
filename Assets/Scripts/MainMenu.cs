using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    private string m_nickName;
    private string m_roomName;
    
    public void SetNickname(string name)
    {
        GameManager.s_nickName = name;
    }
    
    public void SetRoomName(string name)
    {
        GameManager.s_roomName = name;
    }
}
