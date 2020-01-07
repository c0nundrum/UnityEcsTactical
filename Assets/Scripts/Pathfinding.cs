using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;

public class Pathfinding : ComponentSystem
{
    DynamicBuffer<Tile> tileBuffer;

    protected override void OnUpdate()
    {
        Entities.ForEach((DynamicBuffer<MapBuffer> buffer) =>
        {
            //foreach (var element in buffer.Reinterpret<Tile>())
            //{
            //    Debug.Log(element.coordinates);
            //}
            tileBuffer = buffer.Reinterpret<Tile>();
            //Debug.Log(getTileAt(1, 0).coordinates);
        });
    }

    private Tile getTileAt(int x, int y)
    {
        return tileBuffer[y * TileHandler.instance.height + x];
    }
    //private const int MOVE_STRAIGHT_COST = 10;
    //private const int MOVE_DIAGONAL_COST = 14;

    //private PathfindingComponent rootNode;
    //private PathfindingComponent startNode;
    //private PathfindingComponent endNode;

    //private List<PathfindingComponent> openList;
    //private List<PathfindingComponent> closedList;
    //private float2 selectedUnitTranslation;

    //protected override void OnUpdate()
    //{
    //    Entities.WithAllReadOnly<UnitSelected, SSoldier>().ForEach((Entity entity, ref SSoldier soldier) =>
    //    {
    //        this.selectedUnitTranslation = soldier.currentCoordinates;
    //    });

    //    Entities.WithAll<Tile, PathfindingComponent>().ForEach((Entity entity, ref Tile tile, ref PathfindingComponent pathfindingComponent) =>
    //    {
    //        if (tile.coordinates.x == 0 && tile.coordinates.y == 0)
    //        {
    //            this.rootNode = pathfindingComponent;
    //        }
    //        if (tile.coordinates.x == selectedUnitTranslation.x && tile.coordinates.y == selectedUnitTranslation.y)
    //        {
    //            this.startNode = pathfindingComponent;
    //        }
    //    });

    //    //Target
    //    Entities.WithAll<HoverTile, Tile, PathfindingComponent>().ForEach((Entity entity, ref Tile tile, ref PathfindingComponent pathfindingComponent) =>
    //    {
    //        this.endNode = pathfindingComponent;
    //    });
    //}

    //private List<PathfindingComponent> findPath()
    //{
    //    openList = new List<PathfindingComponent>() { startNode };
    //    closedList = new List<PathfindingComponent>();
    //    Entities.WithAll<Tile, PathfindingComponent>().ForEach((Entity entity, ref Tile tile, ref PathfindingComponent pathfindingComponent) =>
    //    {
    //        pathfindingComponent.gCost = int.MaxValue;
    //        pathfindingComponent.cameFromTile = tile;
    //    });

    //    startNode.gCost = 0;
    //    startNode.hCost = CalculateDistanceCost(startNode, endNode);

    //    while (openList.Count > 0)
    //    {
    //        PathfindingComponent currentNode = GetLowestFCostNode(openList);
    //        if (currentNode.Equals(endNode))
    //        {
    //            //Reached final node
    //            return CalculatePath(endNode);
    //        }

    //        openList.Remove(currentNode);
    //        closedList.Add(currentNode);


    //    }
    //}

    //private List<PathfindingComponent> CalculatePath(PathfindingComponent endNode)
    //{
    //    return null;
    //}

    //private List<PathfindingComponent> GetNeighbours(PathfindingComponent currentNode)
    //{
    //    Entities.WithAll<Tile, NeighbourTiles, PathfindingComponent>().ForEach((Entity entity, ref NeighbourTiles neighbours, ref PathfindingComponent pathComponent) =>
    //    {
    //        if (pathComponent.Equals(currentNode))
    //        {
    //            //GetComponentDataFromEntity<Tile>
    //        }
    //    });
    //}

    //private int CalculateDistanceCost(PathfindingComponent a, PathfindingComponent b)
    //{
    //    int xDistance = (int)math.abs(a.coordinates.x - b.coordinates.x);
    //    int yDistance = (int)math.abs(a.coordinates.y - b.coordinates.y);
    //    int remaining = math.abs(xDistance = yDistance);
    //    return MOVE_DIAGONAL_COST * math.min(xDistance, yDistance) + MOVE_STRAIGHT_COST * remaining;
    //}

    //private PathfindingComponent GetLowestFCostNode(List<PathfindingComponent> componentList)
    //{
    //    PathfindingComponent lowestFCostNode = componentList[0];
    //    for (int i = 1; i < componentList.Count; i++)
    //    {
    //        if (componentList[i].getFCost() < lowestFCostNode.getFCost())
    //        {
    //            lowestFCostNode = componentList[i];
    //        }
    //    }
    //    return lowestFCostNode;
    //}

}
