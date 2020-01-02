using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public struct OccupiedTile : IComponentData { }

public class CurrentTileSystem : ComponentSystem
{
    private float2 currentCoordinates = new float2(0, 0);

    protected override void OnUpdate()
    {

        Entities.WithAll<SSoldier>().ForEach((Entity entity, ref SSoldier soldier) => {
            currentCoordinates = soldier.currentCoordinates;
        });

        Entities.WithAll<OccupiedTile, Tile>().ForEach((Entity entity, ref OccupiedTile occupiedTile, ref Tile tile) => {
            if (currentCoordinates.x != tile.coordinates.x || currentCoordinates.y != tile.coordinates.y)
            {
                PostUpdateCommands.RemoveComponent(entity, typeof(OccupiedTile));
            }
        });

        Entities.WithAll<Tile>().WithNone<OccupiedTile>().ForEach((Entity entity, ref Tile tile) => {
            if(currentCoordinates.x == tile.coordinates.x && currentCoordinates.y == tile.coordinates.y)
            {
                PostUpdateCommands.AddComponent(entity, new OccupiedTile { });
            }
        });
    }
}
