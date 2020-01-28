//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using Unity.Entities;
//using Unity.Mathematics;
//using Unity.Jobs;

//public struct OccupiedTile : IComponentData { public Entity entity; }

////TODO - should be updated to use the DynamicBuffer for the mapping coordinates!

//[UpdateAfter(typeof(ActualMovementSystem))]
//public class CurrentTileSystem : ComponentSystem
//{
//    private float2 currentCoordinates = new float2(0, 0);

//    private DynamicBuffer<Entity> entityBuffer;
//    private DynamicBuffer<Entity> tileEntityBuffer;

//    protected override void OnUpdate()
//    {
        
//        Entities.ForEach((DynamicBuffer<EntityBuffer> buffer) =>
//        {
//            entityBuffer = buffer.Reinterpret<Entity>();
//        });

//        Entities.ForEach((DynamicBuffer<MapEntityBuffer> buffer) =>
//        {
//            tileEntityBuffer = buffer.Reinterpret<Entity>();
//        });

//        //Entities.WithAll<SSoldier, UnitSelected>().ForEach((Entity entity, ref SSoldier soldier) => {
//        //    currentCoordinates = soldier.currentCoordinates;
//        //});

//        Entities.WithAll<OccupiedTile, Tile>().ForEach((Entity entity, ref OccupiedTile occupiedTile, ref Tile tile) => {
//            bool isOccupying = false;
//            for (int i = 0; i < entityBuffer.Length; i++)
//            {
//                SSoldier soldier = EntityManager.GetComponentData<SSoldier>(entityBuffer[i]);
//                if(soldier.currentCoordinates.x == tile.coordinates.x && soldier.currentCoordinates.y == tile.coordinates.y)
//                {
//                    isOccupying = true;
//                }
//            }
//            if (!isOccupying)
//            {
//                PostUpdateCommands.RemoveComponent(entity, typeof(OccupiedTile));
//            }
//        });

//        Entities.WithAll<SSoldier>().ForEach((Entity entity, ref SSoldier soldier) =>
//        {
//            Entity tileEntity = tileEntityBuffer[(int)math.floor(soldier.currentCoordinates.y * TileHandler.instance.width + soldier.currentCoordinates.x)];
//            var getOccupyingTile = GetComponentDataFromEntity<OccupiedTile>(true);
//            if (!getOccupyingTile.Exists(tileEntity))
//            {
//                PostUpdateCommands.AddComponent(tileEntity, new OccupiedTile { entity = entity });
//            }
//        });

//        //Entities.WithAll<Tile>().WithNone<OccupiedTile>().ForEach((Entity entity, ref Tile tile) => {
//        //    if (currentCoordinates.x == tile.coordinates.x && currentCoordinates.y == tile.coordinates.y)
//        //    {
//        //        PostUpdateCommands.AddComponent(entity, new OccupiedTile { });
//        //    }
//        //});

//    }
//}
