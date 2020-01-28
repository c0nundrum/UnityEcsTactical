//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using Unity.Entities;
//using Unity.Transforms;
//using Unity.Mathematics;
//using Unity.Collections;
//using Unity.Jobs;

//public struct CanMove : IComponentData { }
//public struct CalculateMoveAreaFlag : IComponentData { }

////Hardest Perfomance hit
////[UpdateAfter(typeof(UnitMoveSystem))]
//[UpdateBefore(typeof(Pathfinding))]
//public class CalculateMoveSystem : ComponentSystem
//{
//    private float2 selectedUnitTranslation;
//    private int unitSpeed;
//    private Entity unitEntity;

//    bool isMoving;

//    DynamicBuffer<Entity> entityBuffer;

//    protected override void OnUpdate()
//    {

//        Entities.ForEach((DynamicBuffer<MapEntityBuffer> buffer) =>
//        {
//            entityBuffer = buffer.Reinterpret<Entity>();
//        });

//        Entities.WithAllReadOnly<UnitSelected, SSoldier, MoveTo>().ForEach((Entity entity, ref SSoldier soldier, ref MoveTo moveTo) =>
//        {
//            this.selectedUnitTranslation = soldier.currentCoordinates;
//            this.unitSpeed = soldier.Movement;
//            this.isMoving = moveTo.longMove;
//            this.unitEntity = entity;
//        });


//        Entities.WithAllReadOnly<CalculateMoveAreaFlag>().ForEach((Entity _en) => {

//            Entities.WithAllReadOnly<CanMove>().ForEach((Entity entity) =>
//            {
//                PostUpdateCommands.RemoveComponent(entity, typeof(CanMove));
//            });

//            List<Entity> canMoveList = new List<Entity>();
//            Entity initialTile = getCurrentEntityAt(selectedUnitTranslation);
//            canMoveList.Add(initialTile);

//            for (int i = 0; i <= unitSpeed; i++)
//            {
//                for (int j = 0; j <= unitSpeed - i; j++)
//                {

//                    //1st Quadrant
//                    if (selectedUnitTranslation.x + j < TileHandler.instance.width && selectedUnitTranslation.y + i < TileHandler.instance.width)
//                    {
//                        Entity e = getCurrentEntityAt(new float2(selectedUnitTranslation.x + j, selectedUnitTranslation.y + i));
//                        Tile t = getTileFromEntity(e);
//                        //PostUpdateCommands.AddComponent(e, new CanMove { });
//                        if (t.walkable)
//                        {
//                            if (!isIsland(e, canMoveList))
//                            {
//                                canMoveList.Add(e);
//                            }
                        
//                        }                   
//                    }

//                    //2nd Quadrant
//                    if (selectedUnitTranslation.x - j >= 0 && selectedUnitTranslation.y + i < TileHandler.instance.width)
//                    {
//                        Entity e = getCurrentEntityAt(new float2(selectedUnitTranslation.x - j, selectedUnitTranslation.y + i));
//                        Tile t = getTileFromEntity(e);
//                        //PostUpdateCommands.AddComponent(e, new CanMove { });
//                        if (t.walkable)
//                        {
//                            if (!isIsland(e, canMoveList))
//                            {
//                                canMoveList.Add(e);
//                            }

//                        }
//                    }


//                    //3rd Quadrant
//                    if (selectedUnitTranslation.x - j >= 0 && selectedUnitTranslation.y - i >= 0)
//                    {
//                        Entity e = getCurrentEntityAt(new float2(selectedUnitTranslation.x - j, selectedUnitTranslation.y - i));
//                        Tile t = getTileFromEntity(e);
//                        //PostUpdateCommands.AddComponent(e, new CanMove { });
//                        if (t.walkable)
//                        {
//                            if (!isIsland(e, canMoveList))
//                            {
//                                canMoveList.Add(e);
//                            }

//                        }
//                    }


//                            //4th Quadrant
//                    if (selectedUnitTranslation.x + j < TileHandler.instance.width && selectedUnitTranslation.y - i >= 0)
//                    {
//                        Entity e = getCurrentEntityAt(new float2(selectedUnitTranslation.x + j, selectedUnitTranslation.y - i));
//                        Tile t = getTileFromEntity(e);
//                        //PostUpdateCommands.AddComponent(e, new CanMove { });
//                        if (t.walkable)
//                        {
//                            if (!isIsland(e, canMoveList))
//                            {
//                                canMoveList.Add(e);
//                            }
//                        }
//                    }

//                }           
           
//            }

//            canMoveList.Remove(initialTile); //We no longer need the initial tile as a legal move

//            foreach (var e in canMoveList)
//            {
//                //PostUpdateCommands.AddComponent(e, new CanMove { });
//                //if we reconstruct the path from e to the player position and then check if the sum of the movement cost is higher than whatever the player has, we can assume its unreacheable
//                this.resetPaths();
//                PathfindingClass pathfindingClass = new PathfindingClass(EntityManager, entityBuffer);
//                List<Entity> path = pathfindingClass.findPath(e, getCurrentEntityAt(selectedUnitTranslation), GetComponentDataFromEntity<PathfindingComponent>());
//                int movementSum = 0;
//                for (int i = 0; i < path.Count -1; i++)
//                //foreach (var tile in path)
//                {
//                    Tile t = getTileFromEntity(path[i]);
//                    movementSum += t.MovementCost;
//                }
//                if(movementSum <= unitSpeed)
//                {
//                    PostUpdateCommands.AddComponent(e, new CanMove { });
//                }
//            }
//            PostUpdateCommands.RemoveComponent(_en, typeof(CalculateMoveAreaFlag));
//        });

//        //Entities.WithAllReadOnly<CanMove>().ForEach((Entity entity) =>
//        //{
//        //    PostUpdateCommands.RemoveComponent(entity, typeof(CanMove));
//        //});

//        //Entities.WithAllReadOnly<OccupiedTile, Tile, NeighbourTiles>().ForEach((Entity entity, ref Tile tile, ref NeighbourTiles neighbourTiles) =>
//        //{
//        //    var selectedUnities = Entities.WithAll<UnitSelected>().ToEntityQuery().CalculateEntityCount();
//        //    if (selectedUnities > 0)
//        //    {
//        //        if (this.selectedUnitTranslation.x == tile.coordinates.x && this.selectedUnitTranslation.y == tile.coordinates.y)
//        //        {
//        //            Entities.WithNone<OccupiedTile>().WithAllReadOnly<Tile, NeighbourTiles>().ForEach((Entity targetEntity, ref Tile targetTile) => {
//        //                if (math.distance(targetTile.coordinates, selectedUnitTranslation) <= unitSpeed && targetTile.walkable)
//        //                {
//        //                    //Debug.Log("Distance = " + math.distance(targetTile.coordinates, selectedUnitTranslation));
//        //                    PostUpdateCommands.AddComponent(targetEntity, new CanMove { });
//        //                }
//        //            });
//        //        }
//        //    }

//        //});


//        //Paint the entities
//        Entities.WithAllReadOnly<CanMove, Tile, NeighbourTiles>().ForEach((Entity entity, ref Tile tile, ref NeighbourTiles neighbourTiles) =>
//        {
//            Graphics.DrawMesh(
//                       TileHandler.instance.tileSelectedMesh,
//                       new Vector3(tile.coordinates.x, tile.coordinates.y, 0f),
//                       Quaternion.identity,
//                       TileHandler.instance.tileSelectedMaterial,
//                       0
//                   );
//        });

//    }

//    private void resetPaths()
//    {
//        Entities.WithAll<Tile, PathfindingComponent>().ForEach((Entity entity, ref Tile tile, ref PathfindingComponent pathfindingComponent) =>
//        {
//            pathfindingComponent.isPath = false;
//            pathfindingComponent.gCost = int.MaxValue;
//            pathfindingComponent.cameFromTile = tile;
//        });
//    }

//    private Tile getTileFromEntity(Entity en)
//    {
//        return EntityManager.GetComponentData<Tile>(en);
//    }

//    private bool isIsland(Entity en, List<Entity> entities)
//    {
//        List<Entity> neighbourList = new List<Entity>();
//        Tile currentNode = getTileFromEntity(en);

//        if (currentNode.coordinates.x - 1 >= 0)
//        {
//            //Left
//            neighbourList.Add(getCurrentEntityAt((int)math.floor(currentNode.coordinates.x - 1), (int)math.floor(currentNode.coordinates.y)));
//            //Left Down
//            if (currentNode.coordinates.y - 1 >= 0) neighbourList.Add(getCurrentEntityAt((int)math.floor(currentNode.coordinates.x - 1), (int)math.floor(currentNode.coordinates.y - 1)));
//            //Left Up
//            if (currentNode.coordinates.y + 1 < TileHandler.instance.height) neighbourList.Add(getCurrentEntityAt((int)math.floor(currentNode.coordinates.x - 1), (int)math.floor(currentNode.coordinates.y + 1)));
//        }
//        if (currentNode.coordinates.x + 1 < TileHandler.instance.width)
//        {
//            //Right
//            neighbourList.Add(getCurrentEntityAt((int)math.floor(currentNode.coordinates.x + 1), (int)math.floor(currentNode.coordinates.y)));
//            //Right Down
//            if (currentNode.coordinates.y - 1 >= 0) neighbourList.Add(getCurrentEntityAt((int)math.floor(currentNode.coordinates.x + 1), (int)math.floor(currentNode.coordinates.y - 1)));
//            //Right Up
//            if (currentNode.coordinates.y + 1 < TileHandler.instance.height) neighbourList.Add(getCurrentEntityAt((int)math.floor(currentNode.coordinates.x + 1), (int)math.floor(currentNode.coordinates.y + 1)));
//        }
//        //Down
//        if (currentNode.coordinates.y - 1 >= 0) neighbourList.Add(getCurrentEntityAt((int)math.floor(currentNode.coordinates.x), (int)math.floor(currentNode.coordinates.y - 1)));
//        //Up
//        if (currentNode.coordinates.y + 1 < TileHandler.instance.height) neighbourList.Add(getCurrentEntityAt((int)math.floor(currentNode.coordinates.x), (int)math.floor(currentNode.coordinates.y + 1)));

//        foreach (var e in entities)
//        {
//            if (neighbourList.Contains(e))
//            {
//                return false;
//            }
//        }

//        return true;
//    }

//    private Tile getCurrentTileAt(int x, int y)
//    {
//        Entity entity = entityBuffer[y * TileHandler.instance.width + x];
//        return EntityManager.GetComponentData<Tile>(entity);
//    }

//    private Tile getCurrentTileAt(float2 coordinates)
//    {
//        Entity entity = entityBuffer[(int) math.floor(coordinates.y * TileHandler.instance.width + coordinates.x)];
//        return EntityManager.GetComponentData<Tile>(entity);
//    }

//    private Entity getCurrentEntityAt(int x, int y)
//    {
//        return entityBuffer[y * TileHandler.instance.width + x];
//    }

//    private Entity getCurrentEntityAt(float2 coordinates)
//    {
//        return entityBuffer[(int)math.floor(coordinates.y * TileHandler.instance.width + coordinates.x)];
//    }

//}

//////TODO - Finish moving this to a job system, pathfinding should also be inside a job for this
////public class MoveJobSystem : JobComponentSystem
////{
////    private struct MoveSystemJob : IJobForEachWithEntity<CalculateMoveAreaFlag>
////    {
////        public void Execute(Entity entity, int index, ref CalculateMoveAreaFlag c0)
////        {
            
////        }
////    }
////    protected override JobHandle OnUpdate(JobHandle inputDeps)
////    {
        
////    }
////}