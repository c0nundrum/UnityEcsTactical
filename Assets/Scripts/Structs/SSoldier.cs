using Unity.Entities;
using Unity.Mathematics;

public struct SSoldier : IComponentData
{
    public float2 currentCoordinates;
    public int Movement;
    public int Initiative;
}
