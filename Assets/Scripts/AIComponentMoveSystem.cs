using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;


[UpdateAfter(typeof(CalculateMoveSystem))]
public class AIComponentMoveSystem : ComponentSystem
{
    private NativeArray<Entity> canMoveTiles;

    //MapBuffer
    private DynamicBuffer<Entity> entityBuffer;

    protected override void OnUpdate()
    {
        Entities.WithAll<CanMove>().ForEach((Entity en, ref Tile tile) => {      });

        Entities.ForEach((DynamicBuffer<MapEntityBuffer> buffer) =>
        {
            entityBuffer = buffer.Reinterpret<Entity>();
        });

        Entities.WithNone<ReadyToHandle>().WithAll<UnitSelected, AIComponent, SSoldier, MoveTo, AwaitActionFlag>().ForEach((Entity entity, ref SSoldier soldier, ref MoveTo moving) => {
        if (!moving.longMove)
        {
                var moveTileCount = Entities.WithAll<CanMove>().ToEntityQuery().CalculateEntityCount();
                canMoveTiles = new NativeArray<Entity>(moveTileCount, Allocator.Temp);
                int loopCounter = 0;
                Entities.WithAll<Tile, CanMove>().ForEach((Entity en, ref Tile tile) =>
                {
                    canMoveTiles[loopCounter] = en;
                    loopCounter++;
                });

                //DEBUG - Unit may choose its own tile to move, therefore not generating a move, causing errors
                Entity chosen = canMoveTiles[(int)math.floor(UnityEngine.Random.Range(0, moveTileCount - 1))];
                Tile chosenTile = EntityManager.GetComponentData<Tile>(chosen);

                Entity startTileEntity = entityBuffer[(int)math.floor(soldier.currentCoordinates.y * TileHandler.instance.width + soldier.currentCoordinates.x)];

                NativeArray<Entity> tileEntityArray = entityBuffer.ToNativeArray(Allocator.TempJob);

                ResetJob resetJob = new ResetJob
                {
                    arrayEntitytile = tileEntityArray,
                    getComponentPathfindingComponentData = GetComponentDataFromEntity<PathfindingComponent>(false),
                    getComponentTileData = GetComponentDataFromEntity<Tile>(true),
                };

                JobHandle resetJobHandle = resetJob.Schedule();

                NativeList<Entity> pathJob = new NativeList<Entity>(6, Allocator.TempJob);
                NativeList<Entity> neighbourList = new NativeList<Entity>(6, Allocator.TempJob);
                NativeList<Entity> closedList = new NativeList<Entity>(Allocator.TempJob);
                NativeList<Entity> openList = new NativeList<Entity>(Allocator.TempJob);

                resetJobHandle.Complete();

                PathFindingJob pathFindingJob = new PathFindingJob
                {
                    height = TileHandler.instance.height,
                    width = TileHandler.instance.width,
                    neighbourList = neighbourList,
                    tileComponenFromData = GetComponentDataFromEntity<Tile>(true),
                    closedList = closedList,
                    openList = openList,
                    entityBuffer = tileEntityArray,
                    IterationLimit = 20,
                    MOVE_DIAGONAL_COST = 14,
                    MOVE_STRAIGHT_COST = 10,
                    pathFindingComponentFromData = GetComponentDataFromEntity<PathfindingComponent>(false),
                    startTileEntity = startTileEntity,
                    targetTileEntity = chosen,
                    path = pathJob

                };

                JobHandle pathJobHandle = pathFindingJob.Schedule();
                pathJobHandle.Complete();

                tileEntityArray.Dispose();
                closedList.Dispose();
                openList.Dispose();
                
                neighbourList.Dispose();

                NativeList<Entity> path = ReverseNativeList(pathJob);
                
                if (path.Length >= 0)
                {
                    DynamicBuffer<MapBuffer> currentPathBuffer = EntityManager.AddBuffer<MapBuffer>(entity);
                    MoveTo moveTo = EntityManager.GetComponentData<MoveTo>(entity);

                    moveTo.positionInMove = 0;
                    PostUpdateCommands.SetComponent<MoveTo>(entity, moveTo);

                    //Element 0 is always the currently occupied tile, therefore not needed
                    for (int i = 1; i < path.Length; i++)
                    {
                        Tile tileFromPath = EntityManager.GetComponentData<Tile>(path[i]);

                        //Check for duplicate tiles
                        if (tileFromPath.coordinates.Equals(EntityManager.GetComponentData<Tile>(path[i - 1]).coordinates)) { continue; }

                        //If its not duplicate, add it
                        currentPathBuffer.Add(tileFromPath);
                    }

                    PostUpdateCommands.AddComponent(entity, new ReadyToMove { Destination = chosenTile });

                }
                else {
                    //Generated null path
                    Debug.Log("Generated Null move");
                }
                pathJob.Dispose();
                PostUpdateCommands.AddComponent(entity, new ReadyToHandle { });

            }

        });
    }

    private NativeList<Entity> ReverseNativeList(NativeList<Entity> arr)
    {
        for (int i = 0; i < arr.Length / 2; i++)
        {
            var tmp = arr[i];
            arr[i] = arr[arr.Length - i - 1];
            arr[arr.Length - i - 1] = tmp;
        }

        return arr;
    }

}