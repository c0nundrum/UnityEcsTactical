using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using System;

public struct TileInfo
{
    public Entity entity;
    public Tile tile;
    public PathfindingComponent pathfindingComponent;
}

public class CalculatePathfindingComponentSystemJob : JobComponentSystem
{
    [BurstCompile]
    private struct AStarPathfinding : IJob
    {       
        [ReadOnly]
        public int MOVE_STRAIGHT_COST;
        [ReadOnly]
        public int MOVE_DIAGONAL_COST;
        [ReadOnly]
        public int width;
        [ReadOnly]
        public int height;
        [ReadOnly]
        public ComponentDataFromEntity<PathfindingComponent> pathFindingComponentFromData;
        [DeallocateOnJobCompletion] public NativeArray<TileInfo> entityBuffer;
        
        public NativeList<TileInfo> openList;
        public NativeList<TileInfo> closedList;
        public NativeList<TileInfo> neighbourList;

        [ReadOnly]
        public CalculatePathfindingFlag pathFinding;

        public int IterationLimit;
        
        //public EntityCommandBuffer.Concurrent entityCommandBuffer;

        [WriteOnly]
        public NativeList<Entity> output; //out


        public void Execute()
        {
            bool found = false;
            Entity startTileEntity = pathFinding.initialPositionEntity;           
            Entity targetTileEntity = pathFinding.finalPositionEntity;

            PathfindingComponent startNode = pathFindingComponentFromData[startTileEntity];
            TileInfo startTileInfo = entityBuffer[(int)math.floor(startNode.coordinates.y * width + startNode.coordinates.x)];
            PathfindingComponent endNode = pathFindingComponentFromData[targetTileEntity];
            TileInfo endTileInfo = entityBuffer[(int)math.floor(endNode.coordinates.y * width + endNode.coordinates.x)];
           
            PathfindingComponent _temp = new PathfindingComponent
            {
                cameFromTile = startTileInfo.tile,
                coordinates = startTileInfo.tile.coordinates,
                gCost = 0,
                hCost = CalculateDistanceCost(startNode, endNode),
                isPath = startTileInfo.pathfindingComponent.isPath
            };

            entityBuffer[(int)math.floor(startNode.coordinates.y * width + startNode.coordinates.x)] = new TileInfo
            {
                pathfindingComponent = _temp,
                entity = startTileInfo.entity,
                tile = startTileInfo.tile
            };

            openList.Add(entityBuffer[(int)math.floor(startNode.coordinates.y * width + startNode.coordinates.x)]);

            while (IterationLimit > 0 && openList.Length > 0 && !found)
            {

                TileInfo currentEntity = GetLowestFCostNode(openList);

                //PathfindingComponent currentNode = pathFindingComponentFromData[currentEntity];

                //if (currentEntity.pathfindingComponent.coordinates.x == endTileInfo.pathfindingComponent.coordinates.x && currentEntity.pathfindingComponent.coordinates.y == endTileInfo.pathfindingComponent.coordinates.y)
                if(currentEntity.entity.Equals(endTileInfo.entity))
                {
                    //Reached final node
                    CalculatePath(currentEntity);
                    found = true;
                    //entityCommandBuffer.RemoveComponent(index, entity, typeof(CalculatePathfindingFlag));
                }

                openList.RemoveAtSwapBack(GetIndex(currentEntity));
                closedList.Add(currentEntity);

                GetNeighbours(currentEntity);

                for (int i = 0; i < neighbourList.Length; i++)
                {
                    //here we are copying the obj, not creating a pointer, therefore it doesnt work
                    PathfindingComponent neighbourNode = neighbourList[i].pathfindingComponent;

                    if (Contains(closedList, neighbourList[i])) continue;
                    if (!neighbourList[i].tile.walkable)
                    {
                        closedList.Add(neighbourList[i]);
                        continue;
                    }

                    int tentativeGCost = currentEntity.pathfindingComponent.gCost + CalculateDistanceCost(currentEntity.pathfindingComponent, neighbourNode) + neighbourList[i].tile.MovementCost; //this was changed to add a move cost to the tile;
                    if (tentativeGCost < neighbourNode.gCost)
                    {
                        PathfindingComponent _pathFindingComponent = new PathfindingComponent
                        {
                            gCost = tentativeGCost,
                            hCost = CalculateDistanceCost(neighbourNode, endNode),
                            isPath = true,
                            cameFromTile = currentEntity.tile,
                            coordinates = neighbourNode.coordinates
                        };

                        TileInfo _tileInfo = new TileInfo
                        {
                            pathfindingComponent = _pathFindingComponent,
                            entity = neighbourList[i].entity,
                            tile = neighbourList[i].tile
                        };

                        neighbourList[i] = _tileInfo;

                        entityBuffer[(int)math.floor(_tileInfo.pathfindingComponent.coordinates.y * width + _tileInfo.pathfindingComponent.coordinates.x)] = neighbourList[i];

                        if (!Contains(openList,neighbourList[i]))
                        {
                            openList.Add(neighbourList[i]);
                        }
                    }


                }
                IterationLimit--;
            }
        }

        private bool Contains(NativeArray<TileInfo> _array, TileInfo key)
        {
            for (int i = 0; i < _array.Length; i++)
            {
                if (_array[i].entity.Equals(key.entity))
                    return true;
            }
            return false;
        }

        private void GetNeighbours(TileInfo currentEntity)
        {
            neighbourList.Clear();
            PathfindingComponent currentNode = currentEntity.pathfindingComponent;

            if (currentNode.coordinates.x - 1 >= 0)
            {
                //Left
                neighbourList.Add(getEntityAt((int)math.floor(currentNode.coordinates.x - 1), (int)math.floor(currentNode.coordinates.y)));
                //Left Down
                if (currentNode.coordinates.y - 1 >= 0) neighbourList.Add(getEntityAt((int)math.floor(currentNode.coordinates.x - 1), (int)math.floor(currentNode.coordinates.y - 1)));
                //Left Up
                if (currentNode.coordinates.y + 1 < height) neighbourList.Add(getEntityAt((int)math.floor(currentNode.coordinates.x - 1), (int)math.floor(currentNode.coordinates.y + 1)));
            }
            if (currentNode.coordinates.x + 1 < width)
            {
                //Right
                neighbourList.Add(getEntityAt((int)math.floor(currentNode.coordinates.x + 1), (int)math.floor(currentNode.coordinates.y)));
                //Right Down
                if (currentNode.coordinates.y - 1 >= 0) neighbourList.Add(getEntityAt((int)math.floor(currentNode.coordinates.x + 1), (int)math.floor(currentNode.coordinates.y - 1)));
                //Right Up
                if (currentNode.coordinates.y + 1 < height) neighbourList.Add(getEntityAt((int)math.floor(currentNode.coordinates.x + 1), (int)math.floor(currentNode.coordinates.y + 1)));
            }
            //Down
            if (currentNode.coordinates.y - 1 >= 0) neighbourList.Add(getEntityAt((int)math.floor(currentNode.coordinates.x), (int)math.floor(currentNode.coordinates.y - 1)));
            //Up
            if (currentNode.coordinates.y + 1 < height) neighbourList.Add(getEntityAt((int)math.floor(currentNode.coordinates.x), (int)math.floor(currentNode.coordinates.y + 1)));

        }

        private int GetIndex(TileInfo key)
        {
            for(int i = 0; i < openList.Length; i++)
            {
                if (openList[i].entity.Equals(key.entity))
                    return i;
            }
            throw new ApplicationException("key not found in list" );
        }

        private TileInfo GetLowestFCostNode(NativeList<TileInfo> entityList)
        {
            TileInfo lowestFCostEntity = entityList[0];
            PathfindingComponent lowestFCostNode = lowestFCostEntity.pathfindingComponent;
            for (int i = 1; i < entityList.Length; i++)
            {
                PathfindingComponent component = entityList[i].pathfindingComponent;
                if (component.gCost + component.hCost < lowestFCostNode.gCost + lowestFCostNode.hCost)
                {
                    lowestFCostEntity = entityList[i];
                    lowestFCostNode = component;
                }
            }
            return lowestFCostEntity;
        }

        private int CalculateDistanceCost(PathfindingComponent a, PathfindingComponent b)
        {
            int xDistance = (int)math.abs(a.coordinates.x - b.coordinates.x);
            int yDistance = (int)math.abs(a.coordinates.y - b.coordinates.y);
            int remaining = math.abs(xDistance - yDistance);
            return MOVE_DIAGONAL_COST * math.min(xDistance, yDistance) + MOVE_STRAIGHT_COST * remaining;
        }

        private void CalculatePath(TileInfo endEntity)
        {
            PathfindingComponent endNode = endEntity.pathfindingComponent;

            output.Add(endEntity.entity);
            output.Add(endNode.cameFromTile.ownerEntity);

            PathfindingComponent currentNode = endNode;

            while (currentNode.isPath)
            {
                output.Add(currentNode.cameFromTile.ownerEntity);
                currentNode = getEntityAt((int)math.floor(currentNode.cameFromTile.coordinates.x), (int)math.floor(currentNode.cameFromTile.coordinates.y)).pathfindingComponent;
            }
        }

        private TileInfo getEntityAt(int x, int y)
        {
            return entityBuffer[y * width + x];
        }

    }

    [BurstCompile]
    private struct PopulateTileInfo : IJobParallelFor
    {
        [WriteOnly]
        public NativeArray<TileInfo> output;

        [ReadOnly]
        [DeallocateOnJobCompletion] public NativeArray<Entity> mapBufferArray;
        [ReadOnly]
        public ComponentDataFromEntity<PathfindingComponent> getPathfindingComponent;
        [ReadOnly]
        public ComponentDataFromEntity<Tile> getTileComponent;

        public void Execute(int index)
        {
            TileInfo tileInfo = new TileInfo
            {
                entity = mapBufferArray[index],
                pathfindingComponent = getPathfindingComponent[mapBufferArray[index]],
                tile = getTileComponent[mapBufferArray[index]]
            };
            output[index] = tileInfo;
        }
    }

    [BurstCompile]
    private struct GetPathfindingEntities : IJobForEachWithEntity<CalculatePathfindingFlag>
    {
        [WriteOnly]
        public CalculatePathfindingFlag output;

        public void Execute(Entity entity, int index, [ReadOnly] ref CalculatePathfindingFlag pathFindingFlag)
        {
            output = pathFindingFlag;
        }
    }

    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;

    protected override void OnCreate()
    {
        endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        base.OnCreate();
    }

    protected override void OnStopRunning()
    {
        if (pathEntities.IsCreated)
            pathEntities.Dispose();
        if (closedList.IsCreated)
            closedList.Dispose();
        if (neighbourList.IsCreated)
            neighbourList.Dispose();
        if (openList.IsCreated)
            openList.Dispose();
        if (pathfindingGroup.IsCreated)
            pathfindingGroup.Dispose();
    }

    private NativeList<Entity> pathEntities;
    private NativeList<TileInfo> closedList;
    private NativeList<TileInfo> neighbourList;
    private NativeList<TileInfo> openList;
    private NativeArray<Entity> pathfindingGroup;

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {

        if (pathEntities.IsCreated)
            pathEntities.Dispose();
        if (closedList.IsCreated)
            closedList.Dispose();
        if (neighbourList.IsCreated)
            neighbourList.Dispose();
        if (openList.IsCreated)
            openList.Dispose();
        if (pathfindingGroup.IsCreated)
            pathfindingGroup.Dispose();

        EntityQuery m_Group = GetEntityQuery(ComponentType.ReadOnly<CalculatePathfindingFlag>());
        pathfindingGroup = m_Group.ToEntityArray(Allocator.TempJob);
        if (pathfindingGroup.Length > 1)
            throw new Exception("found more than one pathfinding");

        if (pathfindingGroup.Length > 0)
        {
            CalculatePathfindingFlag pathfindingFlag = EntityManager.GetComponentData<CalculatePathfindingFlag>(pathfindingGroup[0]);

            if (!pathfindingFlag.finalPositionEntity.Equals(Entity.Null) && !pathfindingFlag.initialPositionEntity.Equals(Entity.Null))
            {
                NativeArray<Entity> mapBuffer = new NativeArray<Entity>(TileHandler.instance.height * TileHandler.instance.width, Allocator.TempJob);

                GetBufferArray getBufferArray = new GetBufferArray
                {
                    output = mapBuffer
                };

                JobHandle bufferjob = getBufferArray.Run(this, inputDeps);

                ResetJob resetJob = new ResetJob
                {
                    arrayEntitytile = mapBuffer,
                    getComponentPathfindingComponentData = GetComponentDataFromEntity<PathfindingComponent>(false),
                    getComponentTileData = GetComponentDataFromEntity<Tile>(true)
                };

                JobHandle resetJobHandle = resetJob.Schedule(inputDeps);
                resetJobHandle.Complete();

                NativeArray<TileInfo> tileInfoArray = new NativeArray<TileInfo>(TileHandler.instance.height * TileHandler.instance.width, Allocator.TempJob);

                PopulateTileInfo populateTileInfoJob = new PopulateTileInfo
                {
                    output = tileInfoArray,
                    getPathfindingComponent = GetComponentDataFromEntity<PathfindingComponent>(true),
                    getTileComponent = GetComponentDataFromEntity<Tile>(true),
                    mapBufferArray = mapBuffer
                };

                JobHandle bufferjobHandle = populateTileInfoJob.Schedule(TileHandler.instance.height * TileHandler.instance.width, 32, bufferjob);
                bufferjobHandle.Complete();

                pathEntities = new NativeList<Entity>(Allocator.TempJob);
                closedList = new NativeList<TileInfo>(Allocator.TempJob);
                neighbourList = new NativeList<TileInfo>(Allocator.TempJob);
                openList = new NativeList<TileInfo>(Allocator.TempJob);

                AStarPathfinding aStarPathfinding = new AStarPathfinding
                {
                    closedList = closedList,
                    neighbourList = neighbourList,
                    openList = openList,
                    entityBuffer = tileInfoArray,
                    height = TileHandler.instance.height,
                    width = TileHandler.instance.width,
                    IterationLimit = 400,
                    MOVE_DIAGONAL_COST = 14,
                    MOVE_STRAIGHT_COST = 10,
                    pathFinding = pathfindingFlag,
                    pathFindingComponentFromData = GetComponentDataFromEntity<PathfindingComponent>(true),
                    output = pathEntities
                };

                JobHandle aStarPathfindingJobHandle = aStarPathfinding.Schedule(bufferjobHandle);
                aStarPathfindingJobHandle.Complete();
                Debug.Log(pathEntities);

                inputDeps = JobHandle.CombineDependencies(inputDeps, aStarPathfindingJobHandle);

            }
        }

        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(inputDeps);
        return inputDeps;
    }
}
