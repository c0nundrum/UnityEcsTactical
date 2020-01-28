using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;

[UpdateAfter(typeof(InputSystem))]
public class PathfindingSystem : JobComponentSystem
{
    private const int MOVE_DIAGONAL_COST = 14;
    private const int MOVE_STRAIGHT_COST = 10;

    [BurstCompile]
    private struct QueueMoveEntity : IJobForEachWithEntity<UnitSelected, CalculateMove>
    {
        
        [ReadOnly]
        public ComponentType calculateMoveType;

        public EntityCommandBuffer.Concurrent commandBuffer;

        public int pathLength;

        [WriteOnly]
        public NativeArray<Entity> currentEntity;

        public void Execute(Entity entity, int index, [ReadOnly] ref UnitSelected c0, ref CalculateMove calculateMove)
        {
            //This is acting up, time to implement solution number two, which is to add the pathbuffer to the entity, but should be done in unit creation and then populated here, because commandbuffers!
            currentEntity[0] = entity;
            MovePath movePath = new MovePath
            {
                moveSpeed = 5f,
                positionInMove = pathLength - 1
            };
            commandBuffer.AddComponent(index, entity, movePath);
            commandBuffer.RemoveComponent(index, entity, calculateMoveType);
        }
    }

    [BurstCompile]
    private struct PopulatePathNodeArray : IJobParallelFor
    {
        [ReadOnly]
        public int2 endPosition;
        [ReadOnly]
        [DeallocateOnJobCompletion] public NativeArray<Entity> mapArray;
        [ReadOnly]
        public ComponentDataFromEntity<Tile> lookup;

        [WriteOnly]
        public NativeArray<PathNode> nodeArray;

        public void Execute(int index)
        {
            Tile tile = lookup[mapArray[index]];

            nodeArray[index] = new PathNode
            {
                x = tile.coordinates.x,
                y = tile.coordinates.y,
                index = index,

                gCost = int.MaxValue,
                hCost = CalculateDistanceCost(new int2(tile.coordinates.x, tile.coordinates.y), endPosition),
                fCost = int.MaxValue + CalculateDistanceCost(new int2(tile.coordinates.x, tile.coordinates.y), endPosition),

                isWalkable = tile.walkable,
                cameFromNodeIndex = -1
            };

        }
        private int CalculateDistanceCost(int2 aPosition, int2 bPosition)
        {
            int xDistance = (int)math.abs(aPosition.x - bPosition.x);
            int yDistance = (int)math.abs(aPosition.y - bPosition.y);
            int remaining = math.abs(xDistance - yDistance);
            return MOVE_DIAGONAL_COST * math.min(xDistance, yDistance) + MOVE_STRAIGHT_COST * remaining;
        }
    }

    [BurstCompile]
    private struct UpdateBuffer : IJob
    {
        [ReadOnly]
        public NativeArray<int2> path;
        [ReadOnly]
        public Entity entity;

        public BufferFromEntity<PathBuffer> lookup;

        public void Execute()
        {
            lookup[entity].Reinterpret<int2>().AddRange(path);
        }
    }

    [BurstCompile]
    private struct RemoveComponent : IJobForEachWithEntity<CalculateMove>
    {
        public EntityCommandBuffer.Concurrent commandBuffer;
        public ComponentType typeOfCalculateMove;

        public void Execute(Entity entity, int index, [ReadOnly] ref CalculateMove c0)
        {
            commandBuffer.RemoveComponent(index, entity, typeOfCalculateMove);
        }
    }

    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;
    private NativeArray<CalculateMove> m_array;
    private NativeList<int2> pathOutput;
    private NativeArray<Entity> currentSelectedEntity;

    protected override void OnCreate()
    {
        endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        base.OnCreate();
    }

    protected override void OnDestroy()
    {
        if (m_array.IsCreated)
            m_array.Dispose();
        if (currentSelectedEntity.IsCreated)
            currentSelectedEntity.Dispose();
        if (pathOutput.IsCreated)
            pathOutput.Dispose();
        base.OnDestroy();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (m_array.IsCreated)
            m_array.Dispose();
        if (pathOutput.IsCreated)
            pathOutput.Dispose();
        if (currentSelectedEntity.IsCreated)
            currentSelectedEntity.Dispose();

        EntityQuery m_GroupCalculateMove = GetEntityQuery(ComponentType.ReadOnly<CalculateMove>());
        if (m_GroupCalculateMove.CalculateEntityCount() > 0 && m_GroupCalculateMove.CalculateEntityCount() < 2)
        {
            //GetCalculate Move Entity
            m_array = m_GroupCalculateMove.ToComponentDataArray<CalculateMove>(Allocator.TempJob);

            EntityQuery e_GroupMap = GetEntityQuery(typeof(MapEntityBuffer));
            NativeArray<Entity> e_array = e_GroupMap.ToEntityArray(Allocator.TempJob);
            
            DynamicBuffer<Entity> mapEntityBuffers = EntityManager.GetBuffer<MapEntityBuffer>(e_array[0]).Reinterpret<Entity>();
            NativeArray<PathNode> pathNodeArray = new NativeArray<PathNode>(mapEntityBuffers.Length, Allocator.TempJob);

            // send the end position and startposition with the calculatemove tag
            PopulatePathNodeArray populatePathNodeArrayJob = new PopulatePathNodeArray
            {
                endPosition = m_array[0].Destination,
                lookup = GetComponentDataFromEntity<Tile>(true),
                mapArray = mapEntityBuffers.ToNativeArray(Allocator.TempJob),
                nodeArray = pathNodeArray
            };

            JobHandle populatePathArrayHandle = populatePathNodeArrayJob.Schedule(mapEntityBuffers.Length, 32);
            //inputDeps = populatePathArrayHandle;
            // calculate the path and add the MovePath component the the entity
            
            
            populatePathArrayHandle.Complete();

            pathOutput = new NativeList<int2>(Allocator.TempJob);

            FindPathJob findPathJob = new FindPathJob
            {
                endPosition = m_array[0].Destination,
                startPosition = m_array[0].StartPosition,
                gridSize = new int2(TileHandler.instance.width, TileHandler.instance.height),
                pathNodeArray = pathNodeArray,
                path = pathOutput
               
            };

            JobHandle findPathJobHandle = findPathJob.Schedule(populatePathArrayHandle);
            //inputDeps = JobHandle.CombineDependencies(inputDeps, populatePathArrayHandle);
            findPathJobHandle.Complete();

            if(pathOutput.Length >= 1)
            {
                //remove the CalculateMove component from the entity - create a foreach job with the unitselected and calculate move components
                currentSelectedEntity = new NativeArray<Entity>(1, Allocator.TempJob);
                QueueMoveEntity queueMoveEntity = new QueueMoveEntity
                {
                    currentEntity = currentSelectedEntity,
                    calculateMoveType = typeof(CalculateMove),
                    commandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
                    pathLength = pathOutput.Length

                };

                JobHandle queueMoveEntityHandle = queueMoveEntity.Schedule(this, JobHandle.CombineDependencies(findPathJobHandle, inputDeps));
                inputDeps = JobHandle.CombineDependencies(inputDeps, queueMoveEntityHandle);

                queueMoveEntityHandle.Complete();

                if (!currentSelectedEntity[0].Equals(Entity.Null))
                {
                    UpdateBuffer updateBuffer = new UpdateBuffer
                    {
                        path = pathOutput.AsArray(),
                        entity = currentSelectedEntity[0],
                        lookup = GetBufferFromEntity<PathBuffer>(false)
                    };
                    inputDeps = updateBuffer.Schedule(inputDeps);
                }
            } else
            {
                RemoveComponent removeComponent = new RemoveComponent
                {
                    commandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
                    typeOfCalculateMove = typeof(CalculateMove)
                };
                inputDeps = removeComponent.Schedule(this, inputDeps);
            }
            e_array.Dispose();
        }

        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(inputDeps);

        

        return inputDeps;
    }

    [BurstCompile]
    private struct FindPathJob : IJob
    {
        public int2 startPosition;
        public int2 endPosition;
        public int2 gridSize;

        [DeallocateOnJobCompletion]public NativeArray<PathNode> pathNodeArray;

        [WriteOnly]
        public NativeList<int2> path;

        public void Execute()
        {
            NativeArray<int2> neighbourOffsetArray = new NativeArray<int2>(8, Allocator.Temp);

            neighbourOffsetArray[0] = new int2(-1, 0);    //Left
            neighbourOffsetArray[1] = new int2(+1, 0);    //Right
            neighbourOffsetArray[2] = new int2(0, +1);    //Up
            neighbourOffsetArray[3] = new int2(0, -1);    //Down
            neighbourOffsetArray[4] = new int2(-1, -1);   //Left Down
            neighbourOffsetArray[5] = new int2(-1, +1);   //Left up
            neighbourOffsetArray[6] = new int2(+1, -1);   //Right Down
            neighbourOffsetArray[7] = new int2(+1, +1);   //Right Up

            int endNodeIndex = CalculateIndex(endPosition.x, endPosition.y, gridSize.x);
            PathNode startNode = pathNodeArray[CalculateIndex(startPosition.x, startPosition.y, gridSize.x)];
            startNode.gCost = 0;
            startNode.CalculateFCost();
            pathNodeArray[startNode.index] = startNode;

            NativeList<int> openList = new NativeList<int>(Allocator.Temp);
            NativeList<int> closedList = new NativeList<int>(Allocator.Temp);

            openList.Add(startNode.index);

            while (openList.Length > 0)
            {
                int currentNodeIndex = GetLowestCostFNodeIndex(openList, pathNodeArray);
                PathNode currentNode = pathNodeArray[currentNodeIndex];

                if (currentNodeIndex == endNodeIndex)
                {
                    //Reached destination!
                    break;
                }

                for (int i = 0; i < openList.Length; i++)
                {
                    if (openList[i] == currentNodeIndex)
                    {
                        openList.RemoveAtSwapBack(i);
                        break;
                    }
                }

                closedList.Add(currentNodeIndex);

                for (int i = 0; i < neighbourOffsetArray.Length; i++)
                {
                    int2 neighbourOffset = neighbourOffsetArray[i];
                    int2 neighbourPosition = new int2(currentNode.x + neighbourOffset.x, currentNode.y + neighbourOffset.y);

                    if (!IsPositionInsideGrid(neighbourPosition, gridSize))
                    {
                        //Not a valid position
                        continue;
                    }

                    int neighbourNodeIndex = CalculateIndex(neighbourPosition.x, neighbourPosition.y, gridSize.x);

                    if (closedList.Contains(neighbourNodeIndex))
                    {
                        //Already searched node
                        continue;
                    }

                    PathNode neighbourNode = pathNodeArray[neighbourNodeIndex];
                    if (!neighbourNode.isWalkable)
                    {
                        // Not Walkable
                        continue;
                    }

                    int2 currentNodePosition = new int2(currentNode.x, currentNode.y);

                    int tentativeGCost = currentNode.gCost + CalculateDistanceCost(currentNodePosition, neighbourPosition);
                    if (tentativeGCost < neighbourNode.gCost)
                    {
                        neighbourNode.cameFromNodeIndex = currentNodeIndex;
                        neighbourNode.gCost = tentativeGCost;
                        neighbourNode.CalculateFCost();
                        pathNodeArray[neighbourNodeIndex] = neighbourNode;
                        if (!openList.Contains(neighbourNode.index))
                        {
                            openList.Add(neighbourNode.index);
                        }
                    }
                }

            }

            PathNode endNode = pathNodeArray[endNodeIndex];
            if (endNode.cameFromNodeIndex == -1)
            {
                //Didnt find a path!
                //Debug.Log("Didnt find a path!");
            }
            else
            {
                //Found a path
                CalculatePath(pathNodeArray, endNode);
            }

            neighbourOffsetArray.Dispose();
            openList.Dispose();
            closedList.Dispose();
        }

        private void CalculatePath(NativeArray<PathNode> pathNodeArray, PathNode endNode)
        {
            if (endNode.cameFromNodeIndex == -1)
            {
                //Didnt find a path!
                //return new NativeList<int2>(Allocator.Temp);
                path = new NativeList<int2>(Allocator.Temp);
            }
            else
            {
                //Found a path!
                //NativeList<int2> path = new NativeList<int2>(Allocator.Temp);
                path.Add(new int2(endNode.x, endNode.y));

                PathNode currentNode = endNode;
                while (currentNode.cameFromNodeIndex != -1)
                {
                    PathNode cameFromNode = pathNodeArray[currentNode.cameFromNodeIndex];
                    path.Add(new int2(cameFromNode.x, cameFromNode.y));
                    currentNode = cameFromNode;
                }

                //return path;
            }
        }

        private bool IsPositionInsideGrid(int2 gridPosition, int2 gridSize)
        {
            return
                gridPosition.x >= 0 &&
                gridPosition.y >= 0 &&
                gridPosition.x < gridSize.x &&
                gridPosition.y < gridSize.y;
        }

        private int CalculateIndex(int x, int y, int gridWidth)
        {
            return x + y * gridWidth;
        }

        private int GetLowestCostFNodeIndex(NativeList<int> openList, NativeArray<PathNode> pathNodeArray)
        {
            PathNode lowestCostPathNode = pathNodeArray[openList[0]];
            for (int i = 1; i < openList.Length; i++)
            {
                PathNode testPathNode = pathNodeArray[openList[i]];
                if (testPathNode.fCost < lowestCostPathNode.fCost)
                {
                    lowestCostPathNode = testPathNode;
                }
            }
            return lowestCostPathNode.index;
        }

        private int CalculateDistanceCost(int2 aPosition, int2 bPosition)
        {
            int xDistance = (int)math.abs(aPosition.x - bPosition.x);
            int yDistance = (int)math.abs(aPosition.y - bPosition.y);
            int remaining = math.abs(xDistance - yDistance);
            return MOVE_DIAGONAL_COST * math.min(xDistance, yDistance) + MOVE_STRAIGHT_COST * remaining;
        }

    }

    private struct PathNode
    {
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

}
