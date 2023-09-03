using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Fusion.Photon.Realtime;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;


public class NetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{

    [SerializeField] private NetworkPrefabRef m_playerPrefab;
    [SerializeField] private NetworkPrefabRef m_networkSceneManagerPrefab;
    [SerializeField] private ChatManager m_chatManager;
    private Dictionary<PlayerRef, NetworkObject> m_spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

    private NetworkRunner m_runner;
    private bool m_isTransitionningScene;
    private float m_delayToTransition = 2.0f;
    private float m_actualTimer;
    [Networked] private bool m_gameStarted { get; set; } = false;
    [Networked] private bool m_sceneFirstInitialised { get; set; } = false;
    
    // Actions
    private bool m_shootButton;
    private bool m_jumpButton;

    
    private int m_previousSceneIndex;

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (player == m_runner.LocalPlayer)
            m_chatManager.SendMessageOnSubscribedChannel("has connected to the game!");
        if (runner.IsServer)
        {
            // Keep track of the player avatars so we can remove it when they disconnect
            ChooseTeamForPlayer(player);
            m_spawnedCharacters.Add(player, m_gameStarted ? SpawnPlayerRelativeToHisTeam(runner, player) : null);
        }
    }

    private NetworkObject SpawnPlayerRelativeToHisTeam(NetworkRunner runner, PlayerRef player)
    {
            Team playerTeam = GameManager.s_playerTeams[player];
            PlayerSpawnType[] spawns = FindObjectsOfType<PlayerSpawnType>();
            PlayerSpawnType playerSpawnType = spawns.First(spawn => spawn.team == playerTeam);
            NetworkObject networkPlayerObject = runner.Spawn(m_playerPrefab, playerSpawnType ? playerSpawnType.transform.position : Vector3.zero, Quaternion.identity, player);
            Player playerBehaviour = networkPlayerObject.GetComponent<Player>();
            playerBehaviour.Team = playerTeam;
            return networkPlayerObject;
    }

    /// <summary>
    /// Assign team regarding the number of players in each team.
    /// </summary>
    /// <param name="player"></param>
    private void ChooseTeamForPlayer(PlayerRef player)
    {
        int blueSize = GameManager.s_playerTeams.Count((player) => player.Value == Team.Blue);
        int redSize = GameManager.s_playerTeams.Count((player) => player.Value == Team.Red);
        if (blueSize < redSize)
        {
            GameManager.s_playerTeams.TryAdd(player, Team.Blue);
        }
        else
        {
            GameManager.s_playerTeams.TryAdd(player, Team.Red);
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        m_chatManager.SendMessageOnSubscribedChannel($"{m_runner.GetPlayerUserId(player)} has disconnected from the game!");
        m_chatManager.SendMessageOnSubscribedChannel("A player has disconnected from the game!");
        // Find and remove the player's character
        if (m_spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
        {
            runner.Despawn(networkObject);
            m_spawnedCharacters.Remove(player);
            GameManager.s_playerTeams.Remove(player);
        }
    }

    /// <summary>
    /// Inputs are sent to the network
    /// </summary>
    /// <param name="runner"></param>
    /// <param name="input"></param>
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        // Blocks input when scene is transitionning
        if (m_isTransitionningScene) return;
        
        NetworkInputData data = new NetworkInputData();

        if (Input.GetKey(KeyCode.Q))
            data.Direction += Vector3.left;
        
        if (Input.GetKey(KeyCode.D))
            data.Direction += Vector3.right;

        if (m_shootButton)
            data.Buttons |= NetworkInputData.SHOOTACTION;
        m_shootButton = false;
        
        if (m_jumpButton)
            data.Buttons |= NetworkInputData.JUMP;
        m_jumpButton = false;

        input.Set(data);
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
    }

    public void OnDisconnectedFromServer(NetworkRunner runner)
    {
        Reset();
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data)
    {
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        StartCoroutine(SceneCameraTransition());
        m_sceneFirstInitialised = true;
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        m_isTransitionningScene = true;
    }

    /// <summary>
    /// This is where the magic happens. The Fusion Runner is created, set and started here.
    /// We also load the main scene (global).
    /// </summary>
    /// <param name="mode"></param>
    /// <param name="nickName"></param>
    /// <param name="roomName"></param>
    public async void StartGame(GameMode mode, string nickName, string roomName)
    {
        // DontDestroyOnLoad(this.gameObject);
        // Create the Fusion runner and let it know that we will be providing user input
        m_runner = gameObject.AddComponent<NetworkRunner>();
        m_runner.ProvideInput = true;
        // Start or join (depends on gamemode) a session with a specific name
        await m_runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            AuthValues = new AuthenticationValues(nickName),
            SessionName = roomName,
            SceneManager = gameObject.AddComponent<CustomSceneLoader>(),
            Initialized = SpawnNetworkSceneManager
        });
        if (m_runner.IsServer)
            AddScene(2);
    }
    
    void SpawnNetworkSceneManager(NetworkRunner runner) {
        runner.Spawn(m_networkSceneManagerPrefab);
    }

    private void Update()
    {
        m_shootButton |= Input.GetKey(KeyCode.Space);
        m_jumpButton |= Input.GetKey(KeyCode.Z);
    }

    // Immediate mode GUI to have easy UI
    private void OnGUI()
    {
        if (GUI.Button(new Rect(Screen.width-200, 0, 200, 40), "Return to Menu"))
        {
            Reset();
        }
        // If i am not server, I don't have authoritative rights
        // We could have implemented a mecanism to send a message to the server for changing the scene
        if (m_runner.IsClient) return;
        
        if (GUI.Button(new Rect(0, 0, 200, 40), "Load level 0"))
        {
            AddScene(2);
            m_gameStarted = false;
        }
        if (GUI.Button(new Rect(0, 50, 200, 40), "Load level 1"))
        {
            AddScene(3);
            m_gameStarted = false;
        }

        if (!m_sceneFirstInitialised || m_gameStarted) return;
        if (GUI.Button(new Rect((Screen.width-200f)/2, (Screen.height-40f)/2, 200, 40), "Ready?"))
        {
            foreach (KeyValuePair<PlayerRef,NetworkObject> spawnedCharacter in m_spawnedCharacters.ToList())
            {
                m_spawnedCharacters[spawnedCharacter.Key] = SpawnPlayerRelativeToHisTeam(m_runner, spawnedCharacter.Key);
            }
            m_gameStarted = true;
        }
    }

    private void Reset()
    {
        CleanForReset();
        m_runner.Shutdown();
        SceneManager.LoadScene(0);
    }

    /// <summary>
    /// Clean the scene by deleting "Don't destroy on load objects"
    /// </summary>
    private void CleanForReset()
    {
        DontDestroyOnLoad[] dontDestroyOnLoads = FindObjectsOfType<DontDestroyOnLoad>();
        foreach (DontDestroyOnLoad obj in dontDestroyOnLoads)
        {
            Destroy(obj.gameObject);
        }
    }
    
    /// <summary>
    /// Despawn all players' character.
    /// </summary>
    /// <param name="runner"></param>
    private void ClearPlayers(NetworkRunner runner)
    {
        foreach ((PlayerRef playerRef, NetworkObject netobj) in m_spawnedCharacters.ToList())
        {
            runner.Despawn(netobj);
        }
    }

    /// <summary>
    /// Implemntation of the camera's movement between two scenes
    /// </summary>
    /// <returns></returns>
    private IEnumerator SceneCameraTransition()
    {
        m_actualTimer = 0;
        GameObject placeHolder = GameObject.FindWithTag("PlaceHolder");
        Camera mainCam = FindObjectOfType<Camera>();
        while (m_actualTimer < m_delayToTransition)
        {
            m_actualTimer += Time.deltaTime;
            float ratio = m_actualTimer / m_delayToTransition;
            
            mainCam.transform.position = Vector3.Lerp(mainCam.transform.position, placeHolder.transform.position, ratio);
            
            yield return null;
        }
        GameObject.Destroy(placeHolder);
        m_isTransitionningScene = false;
        yield break;
    }
    


    /// <summary>
    /// Override of AddScene to handle one scene at a time
    /// </summary>
    /// <param name="sceneRef"></param>
    private void AddScene(SceneRef sceneRef)
    {
        if (m_runner.IsServer)
        {
            ClearPlayers(m_runner);
            CustomNetworkSceneManager sceneManager = m_runner.SceneManager();
            foreach (SceneRef scene in sceneManager.LoadedScenes)
            {
                if (scene != sceneRef)
                    sceneManager.RemoveScene(scene);
            }
            sceneManager.AddScene(sceneRef);
        }
    }
}


