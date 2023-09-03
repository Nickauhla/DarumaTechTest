using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawnType : MonoBehaviour
{
    
    public Team team;
}

[Serializable]
public enum Team
{
    None,
    Red,
    Blue
}
