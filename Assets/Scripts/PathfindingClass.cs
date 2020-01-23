using System.Collections.Generic;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;

[BurstCompile]
public struct ResetJob : IJob
{
    [ReadOnly]
    public NativeArray<Entity> arrayEntitytile;
    [ReadOnly]
    public ComponentDataFromEntity<Tile> getComponentTileData;

    public ComponentDataFromEntity<PathfindingComponent> getComponentPathfindingComponentData;

    public void Execute()
    {
        for (int index = 0; index < arrayEntitytile.Length; index++)
        {
            PathfindingComponent c = getComponentPathfindingComponentData[arrayEntitytile[index]];
            getComponentPathfindingComponentData[arrayEntitytile[index]] = new PathfindingComponent
            {
                coordinates = c.coordinates,
                gCost = int.MaxValue,
                isPath = false,
                cameFromTile = getComponentTileData[arrayEntitytile[index]]
            };
        }
    }
}

[BurstCompile]
public struct ReverseNativeArrayJob : IJob
{
    [ReadOnly]
    public NativeList<Entity> arr;

    [WriteOnly]
    public NativeList<Entity> outArray;

    public void Execute()
    {
        outArray = arr;
        for (int i = 0; i < outArray.Length / 2; i++)
        {
            var tmp = outArray[i];
            outArray[i] = outArray[outArray.Length - i - 1];
            outArray[outArray.Length - i - 1] = tmp;
        }
    }
}

[BurstCompile]
public struct GetBufferArray : IJobForEachWithEntity_EB<MapEntityBuffer>
{
    [WriteOnly]
    public NativeArray<Entity> output;

    public void Execute(Entity entity, int index, DynamicBuffer<MapEntityBuffer> buffer)
    {
        for (int i = 0; i < buffer.Length; i++)
        {
            output[i] = buffer[i];
        }
    }
}

[BurstCompile]
public struct PathFindingJob : IJob
{
    [ReadOnly]
    public int MOVE_STRAIGHT_COST;
    [ReadOnly]
    public int MOVE_DIAGONAL_COST;
    [ReadOnly]
    public int width;
    [ReadOnly]
    public int height;

    public int IterationLimit;

    public NativeList<Entity> openList;
    public NativeList<Entity> closedList;
    public NativeList<Entity> neighbourList;

    //[ReadOnly]
    public ComponentDataFromEntity<PathfindingComponent> pathFindingComponentFromData;
    [ReadOnly]
    public ComponentDataFromEntity<Tile> tileComponenFromData;

    [ReadOnly]
    public NativeArray<Entity> entityBuffer;
    [ReadOnly]
    public Entity startTileEntity;
    [ReadOnly]
    public Entity targetTileEntity;

    [WriteOnly]
    public NativeList<Entity> path; //out

    public void Execute()
    {
        PathfindingComponent startNode = pathFindingComponentFromData[startTileEntity];
        PathfindingComponent endNode = pathFindingComponentFromData[targetTileEntity];

        openList.Add(startTileEntity);

        //startNode.gCost = 0;
        //startNode.hCost = CalculateDistanceCost(startNode, endNode);
        //startNode.cameFromTile = tileComponenFromData[startTileEntity];

        var coordinates = pathFindingComponentFromData[startTileEntity].coordinates;
        pathFindingComponentFromData[startTileEntity] = new PathfindingComponent
        {
            coordinates = coordinates,
            gCost = 0,
            hCost = CalculateDistanceCost(startNode, endNode),
            isPath = startNode.isPath,
            cameFromTile = tileComponenFromData[startTileEntity]
        };

        while (IterationLimit > 0 && openList.Length > 0)
        {

            Entity currentEntity = GetLowestFCostNode(openList);

            PathfindingComponent currentNode = pathFindingComponentFromData[currentEntity];

            if (currentNode.coordinates.x == endNode.coordinates.x && currentNode.coordinates.y == endNode.coordinates.y)
            {
                //Reached final node
                CalculatePath(targetTileEntity);
            }

            openList.RemoveAtSwapBack(openList.IndexOf(currentEntity));
            closedList.Add(currentEntity);

            GetNeighbours(currentEntity);

            for (int i = 0; i < neighbourList.Length; i++)
            {
                //here we are copying the obj, not creating a pointer, therefore it doesnt work
                PathfindingComponent neighbourNode = pathFindingComponentFromData[neighbourList[i]];

                if (closedList.Contains(neighbourList[i])) continue;
                if (!tileComponenFromData[neighbourList[i]].walkable)
                {
                    closedList.Add(neighbourList[i]);
                    continue;
                }

                int tentativeGCost = currentNode.gCost + CalculateDistanceCost(currentNode, neighbourNode) + tileComponenFromData[neighbourList[i]].MovementCost; //this was changed to add a move cost to the tile;
                if (tentativeGCost < neighbourNode.gCost)
                {

                    pathFindingComponentFromData[neighbourList[i]] = new PathfindingComponent
                    {
                        coordinates = neighbourNode.coordinates,
                        gCost = tentativeGCost,
                        hCost = CalculateDistanceCost(neighbourNode, endNode),
                        isPath = true,
                        cameFromTile = tileComponenFromData[currentEntity]
                    };

                    //neighbourNode.gCost = tentativeGCost;
                    //neighbourNode.hCost = CalculateDistanceCost(neighbourNode, endNode);
                    //neighbourNode.isPath = true;
                    //neighbourNode.cameFromTile = tileComponenFromData[currentEntity];

                    if (!openList.Contains(neighbourList[i]))
                    {
                        openList.Add(neighbourList[i]);
                    }
                }
                

            }
            IterationLimit--;
        }
    }

    private void GetNeighbours(Entity currentEntity)
    {
        neighbourList.Clear();
        PathfindingComponent currentNode = pathFindingComponentFromData[currentEntity];

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

    private Entity GetLowestFCostNode(NativeList<Entity> entityList)
    {
        Entity lowestFCostEntity = entityList[0];
        PathfindingComponent lowestFCostNode = pathFindingComponentFromData[entityList[0]];
        for (int i = 1; i < entityList.Length; i++)
        {
            PathfindingComponent component = pathFindingComponentFromData[entityList[i]];
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

    private NativeList<Entity> CalculatePath(Entity endEntity)
    {
        PathfindingComponent endNode = pathFindingComponentFromData[endEntity];

        path.Add(endEntity);
        path.Add(endNode.cameFromTile.ownerEntity);

        PathfindingComponent currentNode = endNode;

        while (currentNode.isPath)
        {
            path.Add(currentNode.cameFromTile.ownerEntity);
            currentNode = pathFindingComponentFromData[getEntityAt((int)math.floor(currentNode.cameFromTile.coordinates.x), (int)math.floor(currentNode.cameFromTile.coordinates.y))];
        }

        return path;
    }

    private Entity getEntityAt(int x, int y)
    {
        return entityBuffer[y * width + x];
    }

}

public class PathfindingClass 
{

    private DynamicBuffer<Entity> entityBuffer;

    private const int MOVE_STRAIGHT_COST = 10;
    private const int MOVE_DIAGONAL_COST = 14;

    private List<Entity> openList;
    private List<Entity> closedList;
    private EntityManager entityManager;

    public PathfindingClass(EntityManager entityManager, DynamicBuffer<Entity> entityBuffer)
    {
        this.entityManager = entityManager;

        //TODO - if tile change system is implemented, we need to pass it everytime or at least update it
        this.entityBuffer = entityBuffer;
    }

    public List<Entity> findPath(Entity startTileEntity, Entity targetTileEntity, ComponentDataFromEntity<PathfindingComponent> componentDataFromEntity)
    {
        PathfindingComponent startNode = entityManager.GetComponentData<PathfindingComponent>(startTileEntity);
        PathfindingComponent endNode = entityManager.GetComponentData<PathfindingComponent>(targetTileEntity);

        openList = new List<Entity>() { startTileEntity };
        closedList = new List<Entity>();
        
        UpdatePathFindingJob job = new UpdatePathFindingJob
        {
            entity = startTileEntity,
            gCost = 0,
            hCost = CalculateDistanceCost(startNode, endNode),
            isPath = startNode.isPath,
            componentFromData = componentDataFromEntity,
            tile = startNode.cameFromTile
        };

        JobHandle jobHandle = job.Schedule();

        jobHandle.Complete();

        while (openList.Count > 0)
        {
            Entity currentEntity = GetLowestFCostNode(openList);

            PathfindingComponent currentNode = entityManager.GetComponentData<PathfindingComponent>(currentEntity);

            if (currentNode.coordinates.x == endNode.coordinates.x && currentNode.coordinates.y == endNode.coordinates.y)
            {
                //Reached final node
                return CalculatePath(targetTileEntity);
            }

            openList.Remove(currentEntity);
            closedList.Add(currentEntity);

            List<Entity> neighbourList = GetNeighbours(currentEntity);

            for (int i = 0; i < neighbourList.Count; i++)
            {
                //here we are copying the obj, not creating a pointer, therefore it doesnt work
                PathfindingComponent neighbourNode = entityManager.GetComponentData<PathfindingComponent>(neighbourList[i]);

                if (closedList.Contains(neighbourList[i])) continue;
                if (!entityManager.GetComponentData<Tile>(neighbourList[i]).walkable)
                {
                    closedList.Add(neighbourList[i]);
                    continue;
                }

                int tentativeGCost = currentNode.gCost + CalculateDistanceCost(currentNode, neighbourNode) + entityManager.GetComponentData<Tile>(neighbourList[i]).MovementCost; //this was changed to add a move cost to the tile;
                if (tentativeGCost < neighbourNode.gCost)
                {
                    //This can deffinitely be a parallell job
                    job = new UpdatePathFindingJob
                    {
                        entity = neighbourList[i],
                        gCost = tentativeGCost,
                        hCost = CalculateDistanceCost(neighbourNode, endNode),
                        isPath = true,
                        componentFromData = componentDataFromEntity,
                        tile = entityManager.GetComponentData<Tile>(currentEntity)
                    };

                    jobHandle = job.Schedule();

                    jobHandle.Complete();

                    if (!openList.Contains(neighbourList[i]))
                    {
                        openList.Add(neighbourList[i]);
                    }
                }
            }

        }

        //Out of nodes on openList
        return null;
    }

    private PathfindingComponent getPathFindingFromEntityAt(int x, int y)
    {
        Entity entity = entityBuffer[y * TileHandler.instance.height + x];
        return entityManager.GetComponentData<PathfindingComponent>(entity);
    }

    private Entity getEntityAt(int x, int y)
    {
        return entityBuffer[y * TileHandler.instance.height + x];
    }

    private List<Entity> CalculatePath(Entity endEntity)
    {
        PathfindingComponent endNode = entityManager.GetComponentData<PathfindingComponent>(endEntity);

        List<Entity> path = new List<Entity>();
        path.Add(endEntity);
        path.Add(endNode.cameFromTile.ownerEntity);

        PathfindingComponent currentNode = endNode;

        while (currentNode.isPath)
        {
            path.Add(currentNode.cameFromTile.ownerEntity);
            currentNode = getPathFindingFromEntityAt((int)math.floor(currentNode.cameFromTile.coordinates.x), (int)math.floor(currentNode.cameFromTile.coordinates.y));
        }
        path.Reverse();

        return path;
    }

    private List<Entity> GetNeighbours(Entity currentEntity)
    {
        List<Entity> neighbourList = new List<Entity>();
        PathfindingComponent currentNode = entityManager.GetComponentData<PathfindingComponent>(currentEntity);

        if (currentNode.coordinates.x - 1 >= 0)
        {
            //Left
            neighbourList.Add(getEntityAt((int)math.floor(currentNode.coordinates.x - 1), (int)math.floor(currentNode.coordinates.y)));
            //Left Down
            if (currentNode.coordinates.y - 1 >= 0) neighbourList.Add(getEntityAt((int)math.floor(currentNode.coordinates.x - 1), (int)math.floor(currentNode.coordinates.y - 1)));
            //Left Up
            if (currentNode.coordinates.y + 1 < TileHandler.instance.height) neighbourList.Add(getEntityAt((int)math.floor(currentNode.coordinates.x - 1), (int)math.floor(currentNode.coordinates.y + 1)));
        }
        if (currentNode.coordinates.x + 1 < TileHandler.instance.width)
        {
            //Right
            neighbourList.Add(getEntityAt((int)math.floor(currentNode.coordinates.x + 1), (int)math.floor(currentNode.coordinates.y)));
            //Right Down
            if (currentNode.coordinates.y - 1 >= 0) neighbourList.Add(getEntityAt((int)math.floor(currentNode.coordinates.x + 1), (int)math.floor(currentNode.coordinates.y - 1)));
            //Right Up
            if (currentNode.coordinates.y + 1 < TileHandler.instance.height) neighbourList.Add(getEntityAt((int)math.floor(currentNode.coordinates.x + 1), (int)math.floor(currentNode.coordinates.y + 1)));
        }
        //Down
        if (currentNode.coordinates.y - 1 >= 0) neighbourList.Add(getEntityAt((int)math.floor(currentNode.coordinates.x), (int)math.floor(currentNode.coordinates.y - 1)));
        //Up
        if (currentNode.coordinates.y + 1 < TileHandler.instance.height) neighbourList.Add(getEntityAt((int)math.floor(currentNode.coordinates.x), (int)math.floor(currentNode.coordinates.y + 1)));

        return neighbourList;

    }

    private int CalculateDistanceCost(PathfindingComponent a, PathfindingComponent b)
    {
        int xDistance = (int)math.abs(a.coordinates.x - b.coordinates.x);
        int yDistance = (int)math.abs(a.coordinates.y - b.coordinates.y);
        int remaining = math.abs(xDistance - yDistance);
        return MOVE_DIAGONAL_COST * math.min(xDistance, yDistance) + MOVE_STRAIGHT_COST * remaining;
    }

    private Entity GetLowestFCostNode(List<Entity> entityList)
    {
        Entity lowestFCostEntity = entityList[0];
        PathfindingComponent lowestFCostNode = entityManager.GetComponentData<PathfindingComponent>(entityList[0]);
        for (int i = 1; i < entityList.Count; i++)
        {
            PathfindingComponent component = entityManager.GetComponentData<PathfindingComponent>(entityList[i]);
            if (component.gCost + component.hCost < lowestFCostNode.gCost + lowestFCostNode.hCost)
            {
                lowestFCostEntity = entityList[i];
                lowestFCostNode = component;
            }
        }
        return lowestFCostEntity;
    }

}

