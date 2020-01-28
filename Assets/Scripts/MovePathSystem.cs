//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using Unity.Entities;
//using Unity.Jobs;
//using Unity.Collections;
//using Unity.Transforms;
//using Unity.Mathematics;

//public class MoveJob : JobComponentSystem
//{
//    private struct MoveSystem : IJobForEachWithEntity<MovePath, SSoldier>
//    {
//        public void Execute(Entity entity, int index, ref MovePath c0, ref SSoldier c1)
//        {
//            throw new System.NotImplementedException();
//        }
//    }

//    protected override JobHandle OnUpdate(JobHandle inputDeps)
//    {

//    }
//}


////[UpdateAfter(typeof(Pathfinding))]
////[UpdateAfter(typeof(AIComponentMoveSystem))]
////[UpdateAfter(typeof(CalculatePathfindingComponentSystemJob))]
//public class ActualMovementSystem : ComponentSystem
//{
//    protected override void OnUpdate()
//    {
//        Entities.WithAll<MovePath, SSoldier>().ForEach((Entity entity, ref MovePath movePath, ref SSoldier soldier) =>
//        {

//            DynamicBuffer<PathBuffer> dynamicBuffer = EntityManager.GetBuffer<PathBuffer>(entity);

//            if (movePath.positionInMove == 0 && dynamicBuffer.Length > 0 && soldier.Movement > 0)
//            {
//                moveTo.longMove = true;

//                moveTo.finalDestination = new float3(readyToMove.Destination.coordinates.x, readyToMove.Destination.coordinates.y, 0f);
//            }

//            if (moveTo.positionInMove < dynamicBuffer.Length && soldier.Movement > 0)
//            {

//                Tile tile = dynamicBuffer[moveTo.positionInMove];

//                soldier.currentCoordinates = new float2(tile.coordinates.x, tile.coordinates.y);


//                moveTo.position = new float3(tile.coordinates.x, tile.coordinates.y, 0f);
//                moveTo.move = true;
//                moveTo.moveCost = tile.MovementCost;

//            }
//            else
//            {
//                dynamicBuffer.RemoveRange(0, dynamicBuffer.Length);

//                moveTo.longMove = false;
//                PostUpdateCommands.AddComponent(entity, new CalculateMoveAreaFlag { });
//                PostUpdateCommands.RemoveComponent(entity, typeof(ReadyToMove));

//            }

//        });
//    }
//}

