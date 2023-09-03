using Fusion;

public class Ball : NetworkBehaviour
{
    [Networked] private TickTimer m_lifeTime { get; set; }
    
    public void Init()
    {
        m_lifeTime = TickTimer.CreateFromSeconds(Runner, 5.0f);
    }
    
    public override void FixedUpdateNetwork()
    {
        if(m_lifeTime.Expired(Runner))
            Runner.Despawn(Object);
        else
            transform.position += 5 * transform.forward * Runner.DeltaTime;
    }
}
