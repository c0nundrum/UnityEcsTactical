using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;

public struct UnitSelected : IComponentData { }

[UpdateAfter(typeof(Pathfinding))]
[UpdateAfter(typeof(AIComponentMoveSystem))]
public class ActualMovementSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        Entities.WithAll<UnitSelected, ReadyToMove, MoveTo, SSoldier>().ForEach((Entity entity, ref ReadyToMove readyToMove, ref MoveTo moveTo, ref SSoldier soldier) => {

            DynamicBuffer<MapBuffer> dynamicBuffer = EntityManager.GetBuffer<MapBuffer>(entity);

            if(moveTo.positionInMove == 0 && dynamicBuffer.Length > 0 && soldier.Movement > 0)
            {
                moveTo.longMove = true;

                moveTo.finalDestination = new float3(readyToMove.Destination.coordinates.x, readyToMove.Destination.coordinates.y, 0f);
            }

            if (moveTo.positionInMove < dynamicBuffer.Length && soldier.Movement > 0)
            {

                Tile tile = dynamicBuffer[moveTo.positionInMove];

                soldier.currentCoordinates = new float2(tile.coordinates.x, tile.coordinates.y);

                
                moveTo.position = new float3(tile.coordinates.x, tile.coordinates.y, 0f);
                moveTo.move = true;
                moveTo.moveCost = tile.MovementCost;

            } else
            {
                dynamicBuffer.RemoveRange(0, dynamicBuffer.Length);

                moveTo.longMove = false;
                PostUpdateCommands.AddComponent(entity, new CalculateMoveAreaFlag { });
                PostUpdateCommands.RemoveComponent(entity, typeof(ReadyToMove));

            }

        });
    }
}

