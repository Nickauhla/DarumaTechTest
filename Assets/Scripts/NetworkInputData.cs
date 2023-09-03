using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public const byte JUMP = 0x01;
    public const byte SHOOTACTION = 0x02;

    public byte Buttons;
    
    // Replace with a bitfield
    public Vector3 Direction;
    
}
