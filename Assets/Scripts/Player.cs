using Fusion;
using UnityEngine;

public class Player : NetworkBehaviour
{
    private NetworkCharacterControllerPrototype m_characterController;
    
    [SerializeField] private Ball m_prefabBall;
    private Vector3 m_lastForwardDirection;
    [Networked] private TickTimer m_delay { get; set; }
    [Networked(OnChanged = nameof(OnTeamChanged))] public Team Team { get; set; }

    private void Awake()
    {
        m_characterController = GetComponent<NetworkCharacterControllerPrototype>();
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetworkInputData data)) return;
        
        // For the character's movement
        data.Direction.Normalize();
        m_characterController.Move(5*data.Direction*Runner.DeltaTime);

        // Delay between each shoot
        if (m_delay.ExpiredOrNotRunning(Runner))
        {
            // Actual shooting
            if (data.Direction.sqrMagnitude > 0)
                m_lastForwardDirection = data.Direction;
            if ((data.Buttons & NetworkInputData.SHOOTACTION) != 0)
            {
                m_delay = TickTimer.CreateFromSeconds(Runner, 0.5f);
                Runner.Spawn(m_prefabBall, transform.position + m_lastForwardDirection,
                    Quaternion.LookRotation(m_lastForwardDirection), Object.InputAuthority, OnBeforeBallSpawned);
            }
        }

        // Jump when action is received
        if ((data.Buttons & NetworkInputData.JUMP) != 0)
        {
            m_characterController.Jump(false);
        }
    }
    
    /// <summary>
    /// Handle the behaviour when team is changed (to colorize the player)
    /// </summary>
    /// <param name="changed"></param>
    public static void OnTeamChanged(Changed<Player> changed)
    {
        Player player = changed.Behaviour;
        switch (player.Team)
        {
            case Team.Red:
                player.material.color = Color.red;
                break;
            case Team.Blue:
                player.material.color = Color.blue;
                break;
            default:
                break;
        }
    }
    
    private Material _material;
    Material material
    {
        get
        {
            if(_material==null)
                _material = GetComponentInChildren<MeshRenderer>().material;
            return _material;
        }
    }

    private void OnBeforeBallSpawned(NetworkRunner runner, NetworkObject o)
    {
        // Initialize the Ball before synchronizing it
        o.GetComponent<Ball>().Init();
    }
}
