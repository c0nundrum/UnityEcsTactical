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
            Entities.WithAll<HoverTile, Tile>().ForEach((Entity entity, ref Tile tile) => {
                this.tile = tile;
            });
            //float3 screenMousePosition = Input.mousePosition;
            //float3 worldMousePosition = Camera.main.ScreenToWorldPoint(screenMousePosition);
            //float3 roundWorldMousePosition = new float3(Mathf.RoundToInt(worldMousePosition.x), Mathf.RoundToInt(worldMousePosition.y), 0);
            //Debug.Log(roundWorldMousePosition);
            Entities.WithAll<UnitSelected>().ForEach((Entity entity, ref MoveTo moveTo, ref SSoldier soldier) => {
                //soldier.currentCoordinates = new float2(roundWorldMousePosition.x, roundWorldMousePosition.y);
                //moveTo.position = roundWorldMousePosition;
                soldier.currentCoordinates = new float2(tile.coordinates.x, tile.coordinates.y);
                moveTo.position = new float3(tile.coordinates.x, tile.coordinates.y, 0f);
                moveTo.move = true;
            });
        }
    }
}
