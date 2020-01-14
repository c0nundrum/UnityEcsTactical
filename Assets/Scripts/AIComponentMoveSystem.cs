using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;

[UpdateAfter(typeof(CalculateMoveSystem))]
public class AIComponentMoveSystem : ComponentSystem
{
    private NativeArray<Entity> canMoveTiles;
    private PathfindingClass pathfinding;

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
                this.resetPaths();
                var moveTileCount = Entities.WithAll<CanMove>().ToEntityQuery().CalculateEntityCount();
                canMoveTiles = new NativeArray<Entity>(moveTileCount, Allocator.Temp);
                int loopCounter = 0;
                Entities.WithAll<Tile, CanMove>().ForEach((Entity en, ref Tile tile) =>
                {
                    canMoveTiles[loopCounter] = en;
                    loopCounter++;
                });
                Entity chosen = canMoveTiles[(int)math.floor(UnityEngine.Random.Range(0, moveTileCount - 1))];

                Tile chosenTile = EntityManager.GetComponentData<Tile>(chosen);
                //Debug.Log(chosenTile.coordinates);

                Entity startTileEntity = entityBuffer[(int)math.floor(soldier.currentCoordinates.y * TileHandler.instance.width + soldier.currentCoordinates.x)];

                List<Entity> path = null;

                pathfinding = new PathfindingClass(EntityManager, entityBuffer);
                path = pathfinding.findPath(startTileEntity, chosen, GetComponentDataFromEntity<PathfindingComponent>(false));

                if (path != null)
                {
                    DynamicBuffer<MapBuffer> currentPathBuffer = EntityManager.AddBuffer<MapBuffer>(entity);
                    MoveTo moveTo = EntityManager.GetComponentData<MoveTo>(entity);

                    moveTo.positionInMove = 0;
                    PostUpdateCommands.SetComponent<MoveTo>(entity, moveTo);

                    //Element 0 is always the currently occupied tile, therefore not needed
                    for (int i = 1; i < path.Count; i++)
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

                PostUpdateCommands.AddComponent(entity, new ReadyToHandle { });

            }

        });
    }

    private void resetPaths()
    {
        Entities.WithAll<Tile, PathfindingComponent>().ForEach((Entity entity, ref Tile tile, ref PathfindingComponent pathfindingComponent) =>
        {
            pathfindingComponent.isPath = false;
            pathfindingComponent.gCost = int.MaxValue;
            pathfindingComponent.cameFromTile = tile;
        });
    }

}
