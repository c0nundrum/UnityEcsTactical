using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public struct UnitSelected : IComponentData { }

public class UnitControl : ComponentSystem
{
    private Tile tile;
    protected override void OnUpdate()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Entities.WithAll<HoverTile, Tile, CanMove>().ForEach((Entity entity, ref Tile tile) => {
                this.tile = tile;
            });
            Entities.WithAll<UnitSelected>().ForEach((Entity entity, ref MoveTo moveTo, ref SSoldier soldier) => {
                soldier.Movement = (int)math.floor(soldier.Movement - math.distance(soldier.currentCoordinates, new float2(tile.coordinates.x, tile.coordinates.y)));
                soldier.currentCoordinates = new float2(tile.coordinates.x, tile.coordinates.y);               
                moveTo.position = new float3(tile.coordinates.x, tile.coordinates.y, 0f);
                moveTo.move = true;
            });
        }
    }
}
