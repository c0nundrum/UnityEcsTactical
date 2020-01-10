using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;

[UpdateAfter(typeof(Pathfinding))]
public class ActualMovementSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        Entities.WithAll<UnitSelected, ReadyToMove, Translation, MoveTo, SSoldier>().ForEach((Entity entity, ref ReadyToMove readyToMove, ref Translation translation, ref MoveTo moveTo, ref SSoldier soldier) => {

            DynamicBuffer<MapBuffer> dynamicBuffer = EntityManager.GetBuffer<MapBuffer>(entity);

            if(moveTo.positionInMove == 0 && dynamicBuffer.Length > 0)
            {
                moveTo.longMove = true;

                moveTo.finalDestination = new float3(readyToMove.Destination.coordinates.x, readyToMove.Destination.coordinates.y, 0f);
            }

            if (moveTo.positionInMove < dynamicBuffer.Length)
            {

                Tile tile = dynamicBuffer[moveTo.positionInMove];

                soldier.currentCoordinates = new float2(tile.coordinates.x, tile.coordinates.y);

                
                moveTo.position = new float3(tile.coordinates.x, tile.coordinates.y, 0f);
                moveTo.move = true;

            } else
            {
                Debug.Log("Called before removing");
                dynamicBuffer.RemoveRange(0, dynamicBuffer.Length);

                moveTo.longMove = false;
                PostUpdateCommands.RemoveComponent(entity, typeof(ReadyToMove));
            }

            //if (dynamicBuffer.Length > 0)
            //{              
            //    Tile tile = dynamicBuffer[0];
            //    soldier.currentCoordinates = new float2(tile.coordinates.x, tile.coordinates.y);
            //    moveTo.finalDestination = new float3(readyToMove.Destination.coordinates.x, readyToMove.Destination.coordinates.y, 0f);
            //    moveTo.position = new float3(tile.coordinates.x, tile.coordinates.y, 0f);
            //    moveTo.move = true;
            //    moveTo.longMove = true;
            //    moveTo.moveCost = tile.MovementCost;
            //    dynamicBuffer.RemoveAt(0);
            //} else
            //{
            //    PostUpdateCommands.RemoveComponent(entity, typeof(ReadyToMove));
            //}

        });
    }
}

