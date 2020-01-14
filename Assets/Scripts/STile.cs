using Unity.Entities;
using Unity.Mathematics;

public struct Tile : IComponentData
{

    public bool walkable;
    public float2 coordinates;
    public Entity ownerEntity;
    public int MovementCost;

}

public struct NeighbourTiles : IComponentData
{
    public Tile nTile;
    public Tile sTile;
    public Tile eTile;
    public Tile neTile;
    public Tile seTile;
    public Tile wTile;
    public Tile nwTile;
    public Tile swTile;

}

public struct PathfindingComponent : IComponentData
{
    public bool isPath;
    public Tile cameFromTile;
    public float2 coordinates;
    public int gCost;
    public int hCost;
    public int getFCost()
    {
        return gCost + hCost;
    }

}

[InternalBufferCapacity(8)]
public struct MapBuffer : IBufferElementData
{
    public Tile tile;

    // The following implicit conversions are optional, but can be convenient.
    public static implicit operator Tile(MapBuffer e)
    {
        return e.tile;
    }

    public static implicit operator MapBuffer(Tile e)
    {
        return new MapBuffer { tile = e };
    }
}

[InternalBufferCapacity(8)]
public struct MapEntityBuffer : IBufferElementData
{
    public Entity entity;

    // The following implicit conversions are optional, but can be convenient.
    public static implicit operator Entity(MapEntityBuffer e)
    {
        return e.entity;
    }

    public static implicit operator MapEntityBuffer(Entity e)
    {
        return new MapEntityBuffer { entity = e };
    }
}

[InternalBufferCapacity(8)]
public struct EntityBuffer : IBufferElementData
{
    public Entity entity;

    // The following implicit conversions are optional, but can be convenient.
    public static implicit operator Entity(EntityBuffer e)
    {
        return e.entity;
    }

    public static implicit operator EntityBuffer(Entity e)
    {
        return new EntityBuffer { entity = e };
    }
}

[InternalBufferCapacity(8)]
public struct PlayerEntityBuffer : IBufferElementData
{
    public Entity entity;

    // The following implicit conversions are optional, but can be convenient.
    public static implicit operator Entity(PlayerEntityBuffer e)
    {
        return e.entity;
    }

    public static implicit operator PlayerEntityBuffer(Entity e)
    {
        return new PlayerEntityBuffer { entity = e };
    }
}

[InternalBufferCapacity(8)]
public struct AiBuffer : IBufferElementData
{
    public Entity entity;

    // The following implicit conversions are optional, but can be convenient.
    public static implicit operator Entity(AiBuffer e)
    {
        return e.entity;
    }

    public static implicit operator AiBuffer(Entity e)
    {
        return new AiBuffer { entity = e };
    }
}