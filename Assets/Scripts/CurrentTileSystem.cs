using Unity.Entities;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Transforms;
using Unity.Burst;
using UnityEngine;

public struct OccupiedTile : IComponentData { public Entity entity; }

public class CurrentTileJobSystem : JobComponentSystem
{
    [BurstCompile]
    private struct CurrentTileUpdate : IJobForEachWithEntity<SSoldier, Translation>
    {
        [ReadOnly]
        [DeallocateOnJobCompletion] public NativeArray<Entity> cleanup;
        [ReadOnly]
        [DeallocateOnJobCompletion] public NativeArray<Entity> mapEntityArray;
        [ReadOnly]
        public ComponentType occupiedTileType;
        [ReadOnly]
        public ComponentDataFromEntity<OccupiedTile> lookupOccupiedTile;
        [ReadOnly]
        public int2 gridSize;

        public EntityCommandBuffer.Concurrent commandBuffer;

        public void Execute(Entity entity, int index, ref SSoldier soldier, [ChangedFilter] ref Translation translation)
        {
            int x = math.frac(translation.Value.x) > 0.5 ? (int)math.ceil(translation.Value.x) : (int)math.floor(translation.Value.x);
            int y = math.frac(translation.Value.y) > 0.5 ? (int)math.ceil(translation.Value.y) : (int)math.floor(translation.Value.y);


            if (soldier.currentCoordinates.x != x || soldier.currentCoordinates.y != y)
            {
                if (lookupOccupiedTile.Exists(mapEntityArray[soldier.currentCoordinates.y * gridSize.x + soldier.currentCoordinates.x]))
                {
                    commandBuffer.RemoveComponent(index, mapEntityArray[soldier.currentCoordinates.y * gridSize.x + soldier.currentCoordinates.x], occupiedTileType);
                }
                //Changed Tiles
                soldier.currentCoordinates.x = x;
                soldier.currentCoordinates.y = y;

                commandBuffer.AddComponent(index, mapEntityArray[y * gridSize.x + x], new OccupiedTile { entity = entity });
            }


        }
    }

    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;

    protected override void OnCreate()
    {
        endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        base.OnCreate();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        EntityQuery e_GroupMap = GetEntityQuery(typeof(MapEntityBuffer));
        NativeArray<Entity> e_array = e_GroupMap.ToEntityArray(Allocator.TempJob);
        NativeArray<Entity> mapEntityArray = EntityManager.GetBuffer<MapEntityBuffer>(e_array[0]).Reinterpret<Entity>().ToNativeArray(Allocator.TempJob);

        CurrentTileUpdate currentTileUpdate = new CurrentTileUpdate
        {
            //gridSize = new int2(TileHandler.instance.width, TileHandler.instance.height),
            gridSize = new int2(10, 10),
            lookupOccupiedTile = GetComponentDataFromEntity<OccupiedTile>(true),
            mapEntityArray = mapEntityArray,
            occupiedTileType = typeof(OccupiedTile),
            commandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
            cleanup = e_array
        };

        inputDeps = currentTileUpdate.Schedule(this, inputDeps);

        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(inputDeps);

        return inputDeps;

    }
}