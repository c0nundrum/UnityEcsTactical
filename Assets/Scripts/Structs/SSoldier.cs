using Unity.Entities;
using Unity.Mathematics;

public struct SSoldier : IComponentData
{
    public int2 currentCoordinates;
    public int Movement;
    public int Initiative;
}
