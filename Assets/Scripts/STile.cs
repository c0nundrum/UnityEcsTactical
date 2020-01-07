using Unity.Entities;
using Unity.Mathematics;

public struct Tile : IComponentData
{

    public bool walkable;
    public float2 coordinates;
    public Entity ownerEntity;

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
