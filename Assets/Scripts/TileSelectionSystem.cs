using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public struct HoverTile : IComponentData { }

public class TileSelectionSystem : JobComponentSystem
{
    [BurstCompile]
    private struct HoverTileJob : IJob
    {
        [ReadOnly]
        public NativeArray<Entity> mapArray;
        [ReadOnly]
        public ComponentDataFromEntity<HoverTile> lookupHoverTile;
        [ReadOnly]
        public float3 mousePosition;
        [ReadOnly]
        public int2 mapSize;

        public EntityCommandBuffer commandbuffer;

        public void Execute()
        {
            //not very performant, chaining ifs
            int x = math.frac(mousePosition.x) > 0.5 ? (int)math.ceil(mousePosition.x) : (int)math.floor(mousePosition.x);
            int y = math.frac(mousePosition.y) > 0.5 ? (int)math.ceil(mousePosition.y) : (int)math.floor(mousePosition.y);
            if (x < mapSize.x && x >= 0 && y < mapSize.y && y >= 0)
            {
                Entity currentEntity = mapArray[y * mapSize.x + x];
                if (!lookupHoverTile.Exists(currentEntity))
                {
                    commandbuffer.AddComponent(currentEntity, new HoverTile { });
                }
            }              
        }
    }

    [BurstCompile]
    private struct RemoveHoverTile : IJobForEachWithEntity<HoverTile>
    {
        public EntityCommandBuffer.Concurrent commandbuffer;

        [ReadOnly]
        public ComponentType hoverTileType;
        [ReadOnly]
        public NativeArray<Entity> mapArray;
        [ReadOnly]
        public float3 mousePosition;
        [ReadOnly]
        public int2 mapSize;

        public void Execute(Entity entity, int index, [ReadOnly] ref HoverTile c0)
        {
            int x = math.frac(mousePosition.x) > 0.5 ? (int)math.ceil(mousePosition.x) : (int)math.floor(mousePosition.x);
            int y = math.frac(mousePosition.y) > 0.5 ? (int)math.ceil(mousePosition.y) : (int)math.floor(mousePosition.y);

            //not very performant, chaining ifs
            if (x < mapSize.x && x >= 0 && y < mapSize.y && y >= 0)
            {
                Entity currentEntity = mapArray[y * mapSize.x + x];
                if (!currentEntity.Equals(entity))
                {
                    commandbuffer.RemoveComponent(index, entity, hoverTileType);
                }
            }       
        }
    }


    private NativeArray<Entity> mapEntityArray;
    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;

    protected override void OnCreate()
    {     
        endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

        base.OnCreate();
    }

    protected override void OnDestroy()
    {
        mapEntityArray.Dispose();
        base.OnDestroy();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (mapEntityArray.IsCreated)
            mapEntityArray.Dispose();

        //inputDeps.Complete();

        float3 screenMousePosition = Input.mousePosition;
        float3 worldMousePosition = Camera.main.ScreenToWorldPoint(screenMousePosition);

        EntityQuery e_GroupMap = GetEntityQuery(typeof(MapEntityBuffer));
        NativeArray<Entity> e_array = e_GroupMap.ToEntityArray(Allocator.TempJob);
        mapEntityArray = EntityManager.GetBuffer<MapEntityBuffer>(e_array[0]).Reinterpret<Entity>().ToNativeArray(Allocator.Persistent);

        RemoveHoverTile removeHoverTileJob = new RemoveHoverTile
        {
            commandbuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
            hoverTileType = typeof(HoverTile),
            mapArray = mapEntityArray,
            mapSize = new int2(TileHandler.instance.width, TileHandler.instance.height),
            mousePosition = worldMousePosition
        };

        inputDeps = removeHoverTileJob.Schedule(this, inputDeps);

        HoverTileJob hoverTileJob = new HoverTileJob
        {
            commandbuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer(),
            lookupHoverTile = GetComponentDataFromEntity<HoverTile>(true),
            mapArray = mapEntityArray,
            mapSize = new int2 (TileHandler.instance.width, TileHandler.instance.height),
            mousePosition = worldMousePosition
        };

        inputDeps = hoverTileJob.Schedule(inputDeps);

        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(inputDeps);

        e_array.Dispose();

        return inputDeps;

    }
}

[UpdateAfter(typeof(TileSelectionSystem))]
public class PaintHoverTiles : ComponentSystem
{
    protected override void OnUpdate()
    {
        Entities.WithAll<HoverTile, Translation>().ForEach((Entity entity, ref Translation translation) =>
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
