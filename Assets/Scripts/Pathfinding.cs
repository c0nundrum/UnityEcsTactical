using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;

//public struct PathTile : IComponentData { }
//public struct OpenTile : IComponentData { }

public struct UpdatePathFindingJob : IJob
{
    [ReadOnly]
    public int gCost;
    [ReadOnly]
    public int hCost;
    [ReadOnly]
    public bool isPath;
    [ReadOnly]
    public Tile tile;

    public ComponentDataFromEntity<PathfindingComponent> componentFromData;

    public Entity entity;

    public void Execute()
    {
       if (componentFromData.Exists(entity))
        {
            var coordinates = componentFromData[entity].coordinates;
            componentFromData[entity] = new PathfindingComponent
            {
                coordinates = coordinates,
                gCost = gCost,
                hCost = hCost,
                isPath = isPath,
                cameFromTile = tile
            };
        }
    }
}

public class Pathfinding : ComponentSystem
{
    //Holds the map
    DynamicBuffer<Tile> tileBuffer;
    DynamicBuffer<Entity> entityBuffer;

    private const int MOVE_STRAIGHT_COST = 10;
    private const int MOVE_DIAGONAL_COST = 14;

    private PathfindingComponent rootNode;
    private PathfindingComponent startNode;
    private Tile startTile;
    private PathfindingComponent endNode;

    private List<Entity> openList;
    private List<Entity> closedList;
    private float2 selectedUnitTranslation;

    private Entity startTileEntity;
    private Entity targetTileEntity;

    private NativeArray<Tile> tilePath;

    protected override void OnUpdate()
    {
        //Updates the current map buffer
        //Entities.ForEach((DynamicBuffer<MapBuffer> buffer) =>
        //{
        //    //foreach (var element in buffer.Reinterpret<Tile>())
        //    //{
        //    //    Debug.Log(element.coordinates);
        //    //}
        //    tileBuffer = buffer.Reinterpret<Tile>();
        //    //Debug.Log(getTileAt(1, 0).coordinates);
        //});

        Entities.ForEach((DynamicBuffer<MapEntityBuffer> buffer) =>
        {
            entityBuffer = buffer.Reinterpret<Entity>();
        });

        Entities.WithAllReadOnly<UnitSelected, SSoldier>().ForEach((Entity entity, ref SSoldier soldier) =>
        {
            this.selectedUnitTranslation = soldier.currentCoordinates;
        });

        Entities.WithAll<OccupiedTile>().ForEach((Entity entity) =>
        {
            this.startTileEntity = entity;
        });

        Entities.WithAll<HoverTile>().ForEach((Entity entity) =>
        {
            this.targetTileEntity = entity;
        });

        //Target
        Entities.WithAll<HoverTile, Tile, PathfindingComponent>().ForEach((Entity entity, ref Tile tile, ref PathfindingComponent pathfindingComponent) =>
        {
            this.endNode = pathfindingComponent;
        });

        if (Input.GetMouseButton(0))
        {
            //THIS IS HOW TO SET DATA
            //foreach (Entity entity in entityBuffer)
            //{
            //    Tile tile = EntityManager.GetComponentData<Tile>(entity);
            //    tile.walkable = false;
            //    PostUpdateCommands.SetComponent<Tile>(entity, tile);
                
            //}
            this.resetPaths();
            List<Entity> path = findPath();
            if (path != null)
            {
                for (int i = 0; i < path.Count - 1; i++)
                {
                    Tile tileFromPath = EntityManager.GetComponentData<Tile>(path[i]);
                    Tile nextTileFromPath = EntityManager.GetComponentData<Tile>(path[i + 1]);
                    Debug.Log("Calling");
                    Debug.DrawLine(new Vector3(tileFromPath.coordinates.x, tileFromPath.coordinates.y, 0), new Vector3(nextTileFromPath.coordinates.x, nextTileFromPath.coordinates.y, 0), Color.red, 5f, false);
                }
            }

        }

    }

    private Tile getTileFromEntityAt(int x, int y)
    {
        Entity entity = entityBuffer[y * TileHandler.instance.height + x];
        return EntityManager.GetComponentData<Tile>(entity);
    }

    private PathfindingComponent getPathFindingFromEntityAt(int x, int y)
    {
        Entity entity = entityBuffer[y * TileHandler.instance.height + x];
        return EntityManager.GetComponentData<PathfindingComponent>(entity);
    }

    private Entity getEntityAt(int x, int y)
    {
        return entityBuffer[y * TileHandler.instance.height + x];    
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

    private List<Entity> findPath()
    {
        PathfindingComponent startNode = EntityManager.GetComponentData<PathfindingComponent>(startTileEntity);
        PathfindingComponent endNode = EntityManager.GetComponentData<PathfindingComponent>(targetTileEntity);

        openList = new List<Entity>() { startTileEntity };
        closedList = new List<Entity>();

        //startNode.gCost = 0;
        //startNode.hCost = CalculateDistanceCost(startNode, endNode);

        UpdatePathFindingJob job = new UpdatePathFindingJob
        {
            entity = startTileEntity,
            gCost = 0,
            hCost = CalculateDistanceCost(startNode, endNode),
            isPath = startNode.isPath,
            componentFromData = GetComponentDataFromEntity<PathfindingComponent>(),
            tile = startNode.cameFromTile
        };

        JobHandle jobHandle = job.Schedule();

        jobHandle.Complete();
        //PostUpdateCommands.SetComponent<PathfindingComponent>(startTileEntity, startNode); //Commit updates to the enitity

        while (openList.Count > 0)
        {
            Entity currentEntity = GetLowestFCostNode(openList);

            PathfindingComponent currentNode = EntityManager.GetComponentData<PathfindingComponent>(currentEntity);

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
                PathfindingComponent neighbourNode = EntityManager.GetComponentData<PathfindingComponent>(neighbourList[i]); 

                if (closedList.Contains(neighbourList[i])) continue;

                int tentativeGCost = currentNode.gCost + CalculateDistanceCost(currentNode, neighbourNode);
                if (tentativeGCost < neighbourNode.gCost)
                {
                    job = new UpdatePathFindingJob
                    {
                        entity = neighbourList[i],
                        gCost = tentativeGCost,
                        hCost = CalculateDistanceCost(neighbourNode, endNode),
                        isPath = true,
                        componentFromData = GetComponentDataFromEntity<PathfindingComponent>(),
                        tile = EntityManager.GetComponentData<Tile>(currentEntity)
                    };

                    jobHandle = job.Schedule();

                    jobHandle.Complete();

                    //neighbourNode.cameFromTile = getTileAt((int)math.floor(currentNode.coordinates.x), (int)math.floor(currentNode.coordinates.y));
                    //neighbourNode.cameFromTile = EntityManager.GetComponentData<Tile>(currentEntity);
                    //neighbourNode.isPath = true;
                    //neighbourNode.gCost = tentativeGCost;
                    //neighbourNode.hCost = CalculateDistanceCost(neighbourNode, endNode);

                    //PostUpdateCommands.SetComponent<PathfindingComponent>(neighbourList[i], neighbourNode); //Commit updates to the enitity

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

    private List<Entity> CalculatePath(Entity endEntity)
    {
        PathfindingComponent endNode = EntityManager.GetComponentData<PathfindingComponent>(endEntity);

        List<Entity> path = new List<Entity>();
        path.Add(endEntity);
        path.Add(endNode.cameFromTile.ownerEntity);

        PathfindingComponent currentNode = endNode;

        while(currentNode.isPath)
        {
            path.Add(currentNode.cameFromTile.ownerEntity);
            currentNode = getPathFindingFromEntityAt((int)math.floor(currentNode.cameFromTile.coordinates.x), (int)math.floor(currentNode.cameFromTile.coordinates.y));
        }
        path.Reverse();

        for (int i = 0; i< path.Count; i++)
        {
            PathfindingComponent node = EntityManager.GetComponentData<PathfindingComponent>(path[i]);
            Debug.Log(node.coordinates);
        }

        foreach(Entity en in path)
        {
            PathfindingComponent node = EntityManager.GetComponentData<PathfindingComponent>(en);
            Debug.Log(node.coordinates);
        }

        return path;
    }

    private List<Entity> GetNeighbours(Entity currentEntity)
    {
        List<Entity> neighbourList = new List<Entity>();
        PathfindingComponent currentNode = EntityManager.GetComponentData<PathfindingComponent>(currentEntity);

        if (currentNode.coordinates.x -1 >= 0)
        {
            //Left
            neighbourList.Add(getEntityAt((int)math.floor(currentNode.coordinates.x - 1), (int)math.floor(currentNode.coordinates.y)));
            //Left Down
            if (currentNode.coordinates.y - 1 >= 0) neighbourList.Add(getEntityAt((int)math.floor(currentNode.coordinates.x - 1), (int)math.floor(currentNode.coordinates.y - 1)));
            //Left Up
            if (currentNode.coordinates.y + 1 < TileHandler.instance.height) neighbourList.Add(getEntityAt((int)math.floor(currentNode.coordinates.x - 1), (int)math.floor(currentNode.coordinates.y + 1)));
        }
        if(currentNode.coordinates.x + 1 < TileHandler.instance.width)
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
        PathfindingComponent lowestFCostNode = EntityManager.GetComponentData<PathfindingComponent>(entityList[0]);
        for (int i = 1; i < entityList.Count; i++)
        {
            PathfindingComponent component = EntityManager.GetComponentData<PathfindingComponent>(entityList[i]);
            if (component.gCost + component.hCost < lowestFCostNode.gCost + lowestFCostNode.hCost)
            {
                lowestFCostEntity = entityList[i];
                lowestFCostNode = component;
            }
        }
        return lowestFCostEntity;
    }

}
