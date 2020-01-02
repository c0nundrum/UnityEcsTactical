using Unity.Entities;
using Unity.Mathematics;

public struct Tile : IComponentData
{
    public bool walkable;
    public float2 coordinates;
}
