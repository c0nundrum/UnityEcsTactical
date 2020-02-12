using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Unity.Transforms;

public class MoveAllowanceSystem : JobComponentSystem
{

    private const int MOVE_DIAGONAL_COST = 14;
    private const int MOVE_STRAIGHT_COST = 10;

    private struct PathNode
    {
        public int moveCost;
        public Entity owner;
        public int x;
        public int y;

        public int index;
        public int gCost;
        public int hCost;
        public int fCost;

        public bool isWalkable;

        public int cameFromNodeIndex;

        public void CalculateFCost()
        {
            fCost = gCost + hCost;
        }
    }

    private struct GetTilesJob : IJobForEachWithEntity<Tile, OccupiedTile>
    {
        [ReadOnly]
        [DeallocateOnJobCompletion] public NativeArray<Entity> mapArray;
        [ReadOnly]
        public int unitMovement;
        [ReadOnly]
        public int2 mapSize;

        [WriteOnly]
        public NativeList<Entity> output;
        [WriteOnly]
        public NativeArray<int2> offsetArray;
        [WriteOnly]
        public NativeArray<int2> sliceSize;

        public void Execute(Entity entity, int index, ref Tile tile, [ReadOnly] ref OccupiedTile c1)
        {
            int2 offset = new int2(0, 0);
            int width = 0;
            int height = 0;
            for (int x = unitMovement + tile.coordinates.x; x >= -unitMovement + tile.coordinates.x; x--)
            {
                for (int y = -unitMovement + tile.coordinates.y; y < unitMovement + tile.coordinates.y; y++)
                {
                    if (IsPositionInsideGrid(new int2(x, y), mapSize))
                    {
                        if (offset.x > x)
                            offset.x = x;
                        if (offset.y > y)
                            offset.y = y;

                        output.Add(mapArray[y * mapSize.x + x]);
                        width = offset.x - x;
                        height = offset.y - y;
                    }
                }
            }

            offsetArray[0] = offset;
            sliceSize[0] = new int2(width, height);
        }

        private bool IsPositionInsideGrid(int2 gridPosition, int2 gridSize)
        {
            return
                gridPosition.x >= 0 &&
                gridPosition.y >= 0 &&
                gridPosition.x < gridSize.x &&
                gridPosition.y < gridSize.y;
        }

    }

    private struct UpdateFlagJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<Entity> mapSliceArray;

        public EntityCommandBuffer.Concurrent commandBuffer;

        public void Execute(int index)
        {
            commandBuffer.AddComponent(index, mapSliceArray[index], new CanMoveToTile { });
        }
    }

    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;
    private NativeList<Entity> output;

    protected override void OnCreate()
    {
        endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

        base.OnCreate();
    }

    protected override void OnDestroy()
    {
        output.Dispose();
        base.OnDestroy();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {

        if (output.IsCreated)
            output.Dispose();

        EntityQuery e_GroupMap = GetEntityQuery(typeof(MapEntityBuffer));
        NativeArray<Entity> e_array = e_GroupMap.ToEntityArray(Allocator.TempJob);

        DynamicBuffer<Entity> mapEntityBuffers = EntityManager.GetBuffer<MapEntityBuffer>(e_array[0]).Reinterpret<Entity>();
        output = new NativeList<Entity>(Allocator.TempJob);

        NativeArray<int2> offsetArray = new NativeArray<int2>(1, Allocator.TempJob);
        NativeArray<int2> sliceSize = new NativeArray<int2>(1, Allocator.TempJob);

        GetTilesJob getTilesJob = new GetTilesJob
        {
            mapArray = mapEntityBuffers.ToNativeArray(Allocator.TempJob),
            mapSize = new int2(TileHandler.instance.width, TileHandler.instance.height),
            unitMovement = 4,
            output = output,
            offsetArray = offsetArray,
            sliceSize = sliceSize
        };

        e_array.Dispose();

        inputDeps = getTilesJob.ScheduleSingle(this, inputDeps);

        //make output a pathnode array, on the pathnode array calculate the distance and add a bool to check if its reachable in the best case scenario,
        // make a new job that parallels calculate if the player can reach the node and returns a bool, not a path, based on which we add or not add the 
        //CanMoveToTile component, after this is working we factor in the tiles movement cost

        inputDeps.Complete();

        UpdateFlagJob updateFlagJob = new UpdateFlagJob
        {
            mapSliceArray = output,
            commandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent()
        };

        inputDeps = updateFlagJob.Schedule(output.Length, 32, inputDeps);

        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(inputDeps);

        return inputDeps;
    }

    ////[BurstCompile]
    //private struct FindPathJob : IJobForEachWithEntity<Tile, OccupiedTile>
    //{
    //    public int unitMovement; // TODO, implement this
    //    public int2 mapSize;
    //    //public int2 offset;

    //    [DeallocateOnJobCompletion] public NativeArray<PathNode> mapNodeArray;

    //    public EntityCommandBuffer.Concurrent commandBuffer;

    //    public void Execute(Entity entity, int index, ref Tile tile, [ReadOnly] ref OccupiedTile c1)
    //    {
    //        NativeList<PathNode> pathNodeArray = new NativeList<PathNode>(Allocator.Temp);
    //        int2 offset = new int2(0, 0);
    //        int width = 0;
    //        int height = 0;
    //        for(int x = unitMovement + tile.coordinates.x; x >= -unitMovement + tile.coordinates.x; x--)
    //        {
    //            for (int y = -unitMovement + tile.coordinates.y; y < unitMovement + tile.coordinates.y; y++)
    //            {
    //                if (IsPositionInsideGrid(new int2(x, y), mapSize))
    //                {
    //                    if (offset.x > x)
    //                        offset.x = x;
    //                    if (offset.y > y)
    //                        offset.y = y;

    //                    pathNodeArray.Add(mapNodeArray[y * mapSize.x + x]);
    //                    width = offset.x - x;
    //                    height = offset.y - y;
    //                }
    //            }
    //        }

    //        if (pathNodeArray.Length == 0)
    //            return;

    //        NativeList<int2> path = new NativeList<int2>(Allocator.Temp);
    //        for(int i = 0; i < pathNodeArray.Length; i++)
    //        {
    //            //we need to update the hcost of the pathNodeArray list with the formula CalculateDistanceCost(new int2(tile.coordinates.x, tile.coordinates.y), endPosition)
    //            for (int j = 0; j < pathNodeArray.Length; j++)
    //            {
    //                PathNode node = pathNodeArray[j];
    //                node.hCost = CalculateDistanceCost(new int2(tile.coordinates.x, tile.coordinates.y), new int2(pathNodeArray[i].x, pathNodeArray[i].y));
    //                pathNodeArray[j] = node;
    //            }
    //            NativeList<int2> temp = FindPath(new int2(tile.coordinates.x - offset.x, tile.coordinates.y - offset.y), new int2(pathNodeArray[i].x - offset.x, pathNodeArray[i].y - offset.y), new int2(width, height), pathNodeArray, offset);

    //            if (temp.Length == 0)
    //                continue;

    //            for (int j = 0; j < temp.Length; j++)
    //            {
    //                if (!path.Contains(temp[j]))
    //                    path.Add(temp[j]);
    //            }
    //        }

    //        if(path.Length > 0)
    //        {
    //            for(int i = 0; i < path.Length; i++)
    //            {
    //                commandBuffer.AddComponent(index, pathNodeArray[(path[i].y - offset.y) * mapSize.x + (path[i].x - offset.x)].owner, new CanMoveToTile { });
    //            }
    //        }

    //    }

    //    private NativeList<int2> FindPath(int2 startPosition, int2 endPosition, int2 gridSize, NativeArray<PathNode> array, int2 offset)
    //    {

    //        NativeArray<PathNode> pathNodeArray = new NativeArray<PathNode>(array.Length, Allocator.Temp);
    //        array.CopyTo(pathNodeArray);

    //        NativeArray<int2> neighbourOffsetArray = new NativeArray<int2>(8, Allocator.Temp);

    //        neighbourOffsetArray[0] = new int2(-1, 0);    //Left
    //        neighbourOffsetArray[1] = new int2(+1, 0);    //Right
    //        neighbourOffsetArray[2] = new int2(0, +1);    //Up
    //        neighbourOffsetArray[3] = new int2(0, -1);    //Down
    //        neighbourOffsetArray[4] = new int2(-1, -1);   //Left Down
    //        neighbourOffsetArray[5] = new int2(-1, +1);   //Left up
    //        neighbourOffsetArray[6] = new int2(+1, -1);   //Right Down
    //        neighbourOffsetArray[7] = new int2(+1, +1);   //Right Up

    //        int endNodeIndex = CalculateIndex(endPosition.x, endPosition.y, gridSize.x, offset);
    //        PathNode startNode = pathNodeArray[CalculateIndex(startPosition.x, startPosition.y, gridSize.x, offset)];
    //        startNode.gCost = 0;
    //        startNode.CalculateFCost();
    //        pathNodeArray[startNode.index] = startNode;

    //        NativeList<int> openList = new NativeList<int>(Allocator.Temp);
    //        NativeList<int> closedList = new NativeList<int>(Allocator.Temp);

    //        openList.Add(startNode.index);

    //        while (openList.Length > 0)
    //        {
    //            int currentNodeIndex = GetLowestCostFNodeIndex(openList, pathNodeArray);
    //            PathNode currentNode = pathNodeArray[currentNodeIndex];

    //            if (currentNodeIndex == endNodeIndex)
    //            {
    //                //Reached destination!
    //                break;
    //            }

    //            for (int i = 0; i < openList.Length; i++)
    //            {
    //                if (openList[i] == currentNodeIndex)
    //                {
    //                    openList.RemoveAtSwapBack(i);
    //                    break;
    //                }
    //            }

    //            closedList.Add(currentNodeIndex);

    //            for (int i = 0; i < neighbourOffsetArray.Length; i++)
    //            {
    //                int2 neighbourOffset = neighbourOffsetArray[i];
    //                int2 neighbourPosition = new int2(currentNode.x + neighbourOffset.x, currentNode.y + neighbourOffset.y);

    //                if (!IsPositionInsideGrid(neighbourPosition, gridSize))
    //                {
    //                    //Not a valid position
    //                    continue;
    //                }

    //                int neighbourNodeIndex = CalculateIndex(neighbourPosition.x, neighbourPosition.y, gridSize.x, offset);

    //                if (closedList.Contains(neighbourNodeIndex))
    //                {
    //                    //Already searched node
    //                    continue;
    //                }

    //                PathNode neighbourNode = pathNodeArray[neighbourNodeIndex];
    //                if (!neighbourNode.isWalkable)
    //                {
    //                    // Not Walkable
    //                    continue;
    //                }

    //                int2 currentNodePosition = new int2(currentNode.x, currentNode.y);

    //                int tentativeGCost = currentNode.gCost + CalculateDistanceCost(currentNodePosition, neighbourPosition);
    //                if (tentativeGCost < neighbourNode.gCost)
    //                {
    //                    neighbourNode.cameFromNodeIndex = currentNodeIndex;
    //                    neighbourNode.gCost = tentativeGCost;
    //                    neighbourNode.CalculateFCost();
    //                    pathNodeArray[neighbourNodeIndex] = neighbourNode;
    //                    if (!openList.Contains(neighbourNode.index))
    //                    {
    //                        openList.Add(neighbourNode.index);
    //                    }
    //                }
    //            }

    //        }

    //        PathNode endNode = pathNodeArray[endNodeIndex];

    //        neighbourOffsetArray.Dispose();
    //        openList.Dispose();
    //        closedList.Dispose();

    //        if (endNode.cameFromNodeIndex == -1)
    //        {
    //            //Didnt find a path!
    //            return new NativeList<int2>(0, Allocator.Temp);
    //        }
    //        else
    //        {
    //            //Found a path
    //            return CalculatePath(pathNodeArray, endNode);
    //        }
    //    }

    //    private NativeList<int2> CalculatePath(NativeArray<PathNode> pathNodeArray, PathNode endNode)
    //    {          
    //        if (endNode.cameFromNodeIndex == -1)
    //        {
    //            //Didnt find a path!
    //            return new NativeList<int2>(0, Allocator.Temp);
    //        }
    //        else
    //        {
    //            //Found a path!
    //            NativeList<int2> path = new NativeList<int2>(Allocator.Temp);
    //            path.Add(new int2(endNode.x, endNode.y));

    //            PathNode currentNode = endNode;
    //            while (currentNode.cameFromNodeIndex != -1)
    //            {
    //                PathNode cameFromNode = pathNodeArray[currentNode.cameFromNodeIndex];
    //                path.Add(new int2(cameFromNode.x, cameFromNode.y));
    //                currentNode = cameFromNode;
    //            }

    //            return path;
    //        }
    //    }

    //    private bool IsPositionInsideGrid(int2 gridPosition, int2 gridSize)
    //    {
    //        return
    //            gridPosition.x >= 0 &&
    //            gridPosition.y >= 0 &&
    //            gridPosition.x < gridSize.x &&
    //            gridPosition.y < gridSize.y;
    //    }

    //    private int CalculateIndex(int x, int y, int gridWidth, int2 offset)
    //    {
    //        return (x - offset.x) + (y - offset.y) * gridWidth;
    //    }

    //    private int GetLowestCostFNodeIndex(NativeList<int> openList, NativeArray<PathNode> pathNodeArray)
    //    {
    //        PathNode lowestCostPathNode = pathNodeArray[openList[0]];
    //        for (int i = 1; i < openList.Length; i++)
    //        {
    //            PathNode testPathNode = pathNodeArray[openList[i]];
    //            if (testPathNode.fCost < lowestCostPathNode.fCost)
    //            {
    //                lowestCostPathNode = testPathNode;
    //            }
    //        }
    //        return lowestCostPathNode.index;
    //    }

    //    private int CalculateDistanceCost(int2 aPosition, int2 bPosition)
    //    {
    //        int xDistance = (int)math.abs(aPosition.x - bPosition.x);
    //        int yDistance = (int)math.abs(aPosition.y - bPosition.y);
    //        int remaining = math.abs(xDistance - yDistance);
    //        return MOVE_DIAGONAL_COST * math.min(xDistance, yDistance) + MOVE_STRAIGHT_COST * remaining;
    //    }
    //}

    //[BurstCompile]
    //private struct PopulatePathNodeArray : IJobParallelFor
    //{
    //    [ReadOnly]
    //    [DeallocateOnJobCompletion] public NativeArray<Entity> mapArray;
    //    [ReadOnly]
    //    public ComponentDataFromEntity<Tile> lookup;

    //    //[WriteOnly]
    //    public NativeArray<PathNode> nodeArray;

    //    public void Execute(int index)
    //    {
    //        Tile tile = lookup[mapArray[index]];

    //        nodeArray[index] = new PathNode
    //        {
    //            x = tile.coordinates.x,
    //            y = tile.coordinates.y,
    //            index = index,

    //            gCost = int.MaxValue,
    //            hCost = 0,
    //            fCost = int.MaxValue ,

    //            isWalkable = tile.walkable,
    //            cameFromNodeIndex = -1
    //        };

    //    }
    //    private int CalculateDistanceCost(int2 aPosition, int2 bPosition)
    //    {
    //        int xDistance = (int)math.abs(aPosition.x - bPosition.x);
    //        int yDistance = (int)math.abs(aPosition.y - bPosition.y);
    //        int remaining = math.abs(xDistance - yDistance);
    //        return MOVE_DIAGONAL_COST * math.min(xDistance, yDistance) + MOVE_STRAIGHT_COST * remaining;
    //    }
    //}

    //private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;

    //protected override void OnCreate()
    //{
    //    endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

    //    base.OnCreate();
    //}

    //protected override JobHandle OnUpdate(JobHandle inputDeps)
    //{

    //    EntityQuery e_GroupMap = GetEntityQuery(typeof(MapEntityBuffer));
    //    NativeArray<Entity> e_array = e_GroupMap.ToEntityArray(Allocator.TempJob);

    //    DynamicBuffer<Entity> mapEntityBuffers = EntityManager.GetBuffer<MapEntityBuffer>(e_array[0]).Reinterpret<Entity>();
    //    NativeArray<PathNode> pathNodeArray = new NativeArray<PathNode>(mapEntityBuffers.Length, Allocator.TempJob);

    //    // send the end position and startposition with the calculatemove tag
    //    PopulatePathNodeArray populatePathNodeArrayJob = new PopulatePathNodeArray
    //    {
    //        lookup = GetComponentDataFromEntity<Tile>(true),
    //        mapArray = mapEntityBuffers.ToNativeArray(Allocator.TempJob),
    //        nodeArray = pathNodeArray
    //    };

    //    inputDeps = populatePathNodeArrayJob.Schedule(mapEntityBuffers.Length, 32, inputDeps);

    //    FindPathJob findPathJob = new FindPathJob
    //    {
    //        commandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
    //        mapSize = new int2(TileHandler.instance.width, TileHandler.instance.height),
    //        unitMovement = 4,
    //        mapNodeArray = pathNodeArray
    //    };

    //    e_array.Dispose();

    //    inputDeps = findPathJob.ScheduleSingle(this, inputDeps);

    //    return inputDeps;
    //}
}

[UpdateAfter(typeof(MoveAllowanceSystem))]
public class PaintMoveableTiles : ComponentSystem
{
    protected override void OnUpdate()
    {
        Entities.WithAll<CanMoveToTile, Translation>().ForEach((Entity entity, ref Translation translation) =>
        {
            float3 entityPosition = translation.Value;

            Graphics.DrawMesh(
                    TileHandler.instance.tileSelectedMesh,
                    entityPosition,
                    Quaternion.identity,
                    TileHandler.instance.tileSelectedMaterial,
                    0
                );

        });
    }
}