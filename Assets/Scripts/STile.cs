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
    public Tile wTile;

}
