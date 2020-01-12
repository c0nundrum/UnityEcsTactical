using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;

public struct CanMove : IComponentData { }

//Hardest Perfomance hit
//[UpdateAfter(typeof(UnitMoveSystem))]
[UpdateBefore(typeof(Pathfinding))]
public class CalculateMoveSystem : ComponentSystem
{
    private float2 selectedUnitTranslation;
    private int unitSpeed;

    protected override void OnUpdate()
    {


        Entities.WithAllReadOnly<UnitSelected, SSoldier>().ForEach((Entity entity, ref SSoldier soldier) =>
        {
            this.selectedUnitTranslation = soldier.currentCoordinates;
            this.unitSpeed = soldier.Movement;
        });

        Entities.WithAllReadOnly<CanMove>().ForEach((Entity entity) =>
        {
            PostUpdateCommands.RemoveComponent(entity, typeof(CanMove));
        });

        Entities.WithAllReadOnly<OccupiedTile, Tile, NeighbourTiles>().ForEach((Entity entity, ref Tile tile, ref NeighbourTiles neighbourTiles) =>
        {
            var selectedUnities = Entities.WithAll<UnitSelected>().ToEntityQuery().CalculateEntityCount();
            if (selectedUnities > 0)
            {
                if (this.selectedUnitTranslation.x == tile.coordinates.x && this.selectedUnitTranslation.y == tile.coordinates.y)
                {
                    Entities.WithNone<OccupiedTile>().WithAllReadOnly<Tile, NeighbourTiles>().ForEach((Entity targetEntity, ref Tile targetTile) => {
                        if (math.distance(targetTile.coordinates, selectedUnitTranslation) <= unitSpeed)
                        {
                            //Debug.Log("Distance = " + math.distance(targetTile.coordinates, selectedUnitTranslation));
                            PostUpdateCommands.AddComponent(targetEntity, new CanMove { });
                        }
                    });
                }
            }
            
        });

        Entities.WithAllReadOnly<CanMove, Tile, NeighbourTiles>().ForEach((Entity entity, ref Tile tile, ref NeighbourTiles neighbourTiles) =>
        {
            Graphics.DrawMesh(
                       TileHandler.instance.tileSelectedMesh,
                       new Vector3(tile.coordinates.x, tile.coordinates.y, 0f),
                       Quaternion.identity,
                       TileHandler.instance.tileSelectedMaterial,
                       0
                   );
        });

    }
}
