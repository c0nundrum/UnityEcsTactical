using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;

public struct PathTile : IComponentData { }
public struct OpenTile : IComponentData { }

public class PathfindingSystem : ComponentSystem
{
    //Holds the map
    DynamicBuffer<Entity> entityBuffer;

    //Consts
    private const int MOVE_STRAIGHT_COST = 10;
    private const int MOVE_DIAGONAL_COST = 14;

    private Entity startEntity;
    private Entity endEntity;

    private float2 selectedUnitTranslation;

    private List<Entity> openList;
    private List<Entity> closedList;

    protected override void OnUpdate()
    {
        ////Updates the current map buffer
        //Entities.ForEach((DynamicBuffer<MapEntityBuffer> buffer) =>
        //{
        //    entityBuffer = buffer.Reinterpret<Entity>();
        //});

        //Entities.WithAllReadOnly<UnitSelected, SSoldier>().ForEach((Entity entity, ref SSoldier soldier) =>
        //{
        //    this.selectedUnitTranslation = soldier.currentCoordinates;
        //});

        //Entities.WithAll<OccupiedTile>().ForEach((Entity entity) =>
        //{
        //    this.startEntity = entity;
        //});

        //Entities.WithAll<HoverTile>().ForEach((Entity entity) =>
        //{
        //    this.endEntity = entity;
        //});

        //if (Input.GetMouseButton(0))
        //{
        //    this.resetPaths();

        //    foreach(Entity entity in entityBuffer)
        //    {
        //        Tile tile = EntityManager.GetComponentData<Tile>(entity);
        //        tile.walkable = false;
        //    }
        //    //Entities.ForEach((Entity en, ref Tile tile) => { tile.walkable = false; });
            

        //}
    }

    private void resetPaths()
    {
        Entities.WithAll<Tile, PathfindingComponent>().ForEach((Entity entity, ref Tile tile, ref PathfindingComponent pathfindingComponent) =>
        {
            pathfindingComponent.isPath = false;
            pathfindingComponent.gCost = int.MaxValue;
            pathfindingComponent.cameFromTile = tile;
        });
        Entities.WithAll<OpenTile>().ForEach((Entity entity, ref Tile tile, ref PathfindingComponent pathfindingComponent) =>
        {
            PostUpdateCommands.RemoveComponent(entity, typeof(OpenTile));
        });
    }

    private List<Tile> findPath()
    {
        openList = new List<Entity>() { startEntity };
        closedList = new List<Entity>();

        Entities.ForEach((Entity startEntity, ref PathfindingComponent startNode) => {
            startNode.gCost = 0;
            startNode.hCost = CalculateDistanceCost(startEntity, startEntity);
        });
        return null;
    }

    private int CalculateDistanceCost(Entity a, Entity b)
    {
        //    float2 coordinatesA = new float2(0, 0);
        //    float2 coordinatesB = new float2(0, 0);
        //    Entities.ForEach((Entity a, ref Tile tile) => {
        //        coordinatesA = tile.coordinates;
        //    });
        //    int xDistance = (int)math.abs(coordinatesA.x - b.coordinates.x);
        //    int yDistance = (int)math.abs(a.coordinates.y - b.coordinates.y);
        //    int remaining = math.abs(xDistance - yDistance);
        //    return MOVE_DIAGONAL_COST * math.min(xDistance, yDistance) + MOVE_STRAIGHT_COST * remaining;
        return 0;
    }

}