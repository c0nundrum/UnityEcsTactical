using Unity.Entities;
using Unity.Mathematics;

public struct MoveTo : IComponentData
{
    public bool move;
    public bool longMove;
    public int positionInMove;
    public float3 position;
    public float3 finalDestination;
    public float3 lastMoveDir;
    public float moveSpeed;
    public int moveCost;

}

