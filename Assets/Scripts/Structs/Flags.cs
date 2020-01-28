using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;

public struct ReadyToMove : IComponentData {
    public int2 Destination;
    
}

public struct UnitSelected : IComponentData { }

public struct CalculateMove : IComponentData {
    public int2 Destination;
    public int2 StartPosition;
}

public struct MovePath : IComponentData {
    public int positionInMove;
    public float moveSpeed;
}

public struct UnitFinishedMove : IComponentData { }
