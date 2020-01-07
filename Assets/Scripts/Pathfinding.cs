using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;

public class Pathfinding : ComponentSystem
{
    //Holds the map
    DynamicBuffer<Tile> tileBuffer;

    private const int MOVE_STRAIGHT_COST = 10;
    private const int MOVE_DIAGONAL_COST = 14;

    private PathfindingComponent rootNode;
    private PathfindingComponent startNode;
    private PathfindingComponent endNode;

    private List<PathfindingComponent> openList;
    private List<PathfindingComponent> closedList;
    private float2 selectedUnitTranslation;

    private NativeArray<Tile> tilePath;

    protected override void OnUpdate()
    {
        //Updates the current map buffer
        Entities.ForEach((DynamicBuffer<MapBuffer> buffer) =>
        {
            //foreach (var element in buffer.Reinterpret<Tile>())
            //{
            //    Debug.Log(element.coordinates);
            //}
            tileBuffer = buffer.Reinterpret<Tile>();
            //Debug.Log(getTileAt(1, 0).coordinates);
        });

        Entities.WithAllReadOnly<UnitSelected, SSoldier>().ForEach((Entity entity, ref SSoldier soldier) =>
        {
            this.selectedUnitTranslation = soldier.currentCoordinates;
        });

        Entities.WithAll<Tile, PathfindingComponent>().ForEach((Entity entity, ref Tile tile, ref PathfindingComponent pathfindingComponent) =>
        {
            if (tile.coordinates.x == selectedUnitTranslation.x && tile.coordinates.y == selectedUnitTranslation.y)
            {
                this.startNode = pathfindingComponent;
            }
        });

        //Target
        Entities.WithAll<HoverTile, Tile, PathfindingComponent>().ForEach((Entity entity, ref Tile tile, ref PathfindingComponent pathfindingComponent) =>
        {
            this.endNode = pathfindingComponent;
        });

        if (Input.GetMouseButton(0))
        {
            //List<Tile> path = findPath();
            //if(path != null)
            //{
            //    for (int i = 0; i < path.Count - 1; i++)
            //    {
            //        Debug.DrawLine(new Vector3(path[i].coordinates.x, path[i].coordinates.y) * 10f + Vector3.one * 5f, new Vector3(path[i + 1].coordinates.x, path[i + 1].coordinates.y) * 10f + Vector3.one * 5f, Color.green);
            //        //Debug.Log("called");
            //    }
            //}
            float3 screenMousePosition = Input.mousePosition;
            float3 worldMousePosition = Camera.main.ScreenToWorldPoint(screenMousePosition);
            Tile tile = getTileAt((int) math.floor(worldMousePosition.x), (int)math.floor(worldMousePosition.y));
            Debug.Log(worldMousePosition.x % 1);

        }

    }

    private PathfindingComponent getPathFindingComponentAt(int x, int y)
    {
        Tile tile = getTileAt(x, y);

        ComponentDataFromEntity<PathfindingComponent> myTypeFromEntity = GetComponentDataFromEntity<PathfindingComponent>(false);

        if (myTypeFromEntity.Exists(tile.ownerEntity))
        {
            return myTypeFromEntity[tile.ownerEntity];
        } else
        {
            Debug.Log("Missed one cell at:" + tile.coordinates);
            return new PathfindingComponent { coordinates = new float2(-1, -1) };
        }

    }

    private Tile getTileAt(int x, int y)
    {
        return tileBuffer[y * TileHandler.instance.height + x];
    }



    private List<Tile> findPath()
    {
        openList = new List<PathfindingComponent>() { startNode };
        closedList = new List<PathfindingComponent>();
        Entities.WithAll<Tile, PathfindingComponent>().ForEach((Entity entity, ref Tile tile, ref PathfindingComponent pathfindingComponent) =>
        {
            pathfindingComponent.isPath = false;
            pathfindingComponent.gCost = int.MaxValue;
            pathfindingComponent.cameFromTile = tile;
        });

        startNode.gCost = 0;
        startNode.hCost = CalculateDistanceCost(startNode, endNode);

        while (openList.Count > 0)
        {
            PathfindingComponent currentNode = GetLowestFCostNode(openList);
            if (currentNode.Equals(endNode))
            {
                //Reached final node
                return CalculatePath(endNode);
            }

            openList.Remove(currentNode);
            closedList.Add(currentNode);

            List<PathfindingComponent> neighbourList = GetNeighbours(currentNode);

            for (int i = 0; i < neighbourList.Count; i++)
            {
                PathfindingComponent neighbourNode = neighbourList[i];

                if (closedList.Contains(neighbourNode)) continue;

                int tentativeGCost = currentNode.gCost + CalculateDistanceCost(currentNode, neighbourNode);
                if (tentativeGCost < neighbourNode.gCost)
                {
                    neighbourNode.cameFromTile = getTileAt((int)math.floor(currentNode.coordinates.x), (int)math.floor(currentNode.coordinates.y));
                    neighbourNode.isPath = true;
                    neighbourNode.gCost = tentativeGCost;
                    neighbourNode.hCost = CalculateDistanceCost(neighbourNode, endNode);

                    if (!openList.Contains(neighbourNode))
                    {
                        openList.Add(neighbourNode);
                    }
                }
            }

        }

        //Out of nodes on openList
        return null;
    }

    private List<Tile> CalculatePath(PathfindingComponent endNode)
    {
        //NativeArray<Tile> nativePath = new NativeArray<Tile>();
        List<Tile> path = new List<Tile>();
        path.Add(endNode.cameFromTile);

        PathfindingComponent currentNode = endNode;
        while(currentNode.isPath)
        {
            path.Add(currentNode.cameFromTile);
            currentNode = getPathFindingComponentAt((int)math.floor(currentNode.cameFromTile.coordinates.x), (int)math.floor(currentNode.cameFromTile.coordinates.y));
        }
        path.Reverse();
        return path;
    }

    private List<PathfindingComponent> GetNeighbours(PathfindingComponent currentNode)
    {
        List<PathfindingComponent> neighbourList = new List<PathfindingComponent>();
        if(currentNode.coordinates.x -1 >= 0)
        {
            //Left
            neighbourList.Add(getPathFindingComponentAt((int)math.floor(currentNode.coordinates.x - 1), (int)math.floor(currentNode.coordinates.y)));
            //Left Down
            if (currentNode.coordinates.y - 1 >= 0) neighbourList.Add(getPathFindingComponentAt((int)math.floor(currentNode.coordinates.x - 1), (int)math.floor(currentNode.coordinates.y - 1)));
            //Left Up
            if (currentNode.coordinates.y + 1 < TileHandler.instance.height) neighbourList.Add(getPathFindingComponentAt((int)math.floor(currentNode.coordinates.x - 1), (int)math.floor(currentNode.coordinates.y + 1)));
        }
        if(currentNode.coordinates.x + 1 < TileHandler.instance.width)
        {
            //Right
            neighbourList.Add(getPathFindingComponentAt((int)math.floor(currentNode.coordinates.x + 1), (int)math.floor(currentNode.coordinates.y)));
            //Right Down
            if (currentNode.coordinates.y - 1 >= 0) neighbourList.Add(getPathFindingComponentAt((int)math.floor(currentNode.coordinates.x + 1), (int)math.floor(currentNode.coordinates.y - 1)));
            //Right Up
            if (currentNode.coordinates.y + 1 < TileHandler.instance.height) neighbourList.Add(getPathFindingComponentAt((int)math.floor(currentNode.coordinates.x + 1), (int)math.floor(currentNode.coordinates.y + 1)));
        }
        //Down
        if (currentNode.coordinates.y - 1 >= 0) neighbourList.Add(getPathFindingComponentAt((int)math.floor(currentNode.coordinates.x), (int)math.floor(currentNode.coordinates.y - 1)));
        //Up
        if (currentNode.coordinates.y + 1 < TileHandler.instance.height) neighbourList.Add(getPathFindingComponentAt((int)math.floor(currentNode.coordinates.x), (int)math.floor(currentNode.coordinates.y + 1)));

        return neighbourList;
    }

    private int CalculateDistanceCost(PathfindingComponent a, PathfindingComponent b)
    {
        int xDistance = (int)math.abs(a.coordinates.x - b.coordinates.x);
        int yDistance = (int)math.abs(a.coordinates.y - b.coordinates.y);
        int remaining = math.abs(xDistance = yDistance);
        return MOVE_DIAGONAL_COST * math.min(xDistance, yDistance) + MOVE_STRAIGHT_COST * remaining;
    }

    private PathfindingComponent GetLowestFCostNode(List<PathfindingComponent> componentList)
    {
        PathfindingComponent lowestFCostNode = componentList[0];
        for (int i = 1; i < componentList.Count; i++)
        {
            if (componentList[i].getFCost() < lowestFCostNode.getFCost())
            {
                lowestFCostNode = componentList[i];
            }
        }
        return lowestFCostNode;
    }

}
