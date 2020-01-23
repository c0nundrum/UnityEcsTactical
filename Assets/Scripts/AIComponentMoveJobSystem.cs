using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;

public struct CalculatePathfindingFlag : IComponentData {
    public Entity initialPositionEntity;
    public Entity finalPositionEntity;
}

public class AIComponentMoveJobSystem : JobComponentSystem
{
    [BurstCompile]
    [RequireComponentTag(typeof(UnitSelected), typeof(AIComponent), typeof(AwaitActionFlag))]
    [ExcludeComponent(typeof(ReadyToHandle))]
    private struct PrepareEntityToMove : IJobForEachWithEntity<SSoldier, MoveTo>
    {
        public EntityCommandBuffer.Concurrent entityCommandBuffer;

        [ReadOnly]
        [DeallocateOnJobCompletion] public NativeArray<Entity> canMoveTiles;
        [ReadOnly]
        [DeallocateOnJobCompletion] public NativeArray<Entity> mapEntityBuffer;
        [ReadOnly]
        public int width;
        [ReadOnly]
        public int height;
        [ReadOnly]
        public int randomSeed;

        [ReadOnly]
        public ComponentDataFromEntity<Tile> getComponentDataFromEntity;

        public void Execute(Entity entity, int index, [ReadOnly] ref SSoldier soldier, ref MoveTo moving)
        {
            if (!moving.longMove)
            {
                Entity chosen = canMoveTiles[randomSeed];
                Tile chosenTile = getComponentDataFromEntity[chosen];
                Entity startTileEntity = mapEntityBuffer[(int)math.floor(soldier.currentCoordinates.y * width + soldier.currentCoordinates.x)];
                entityCommandBuffer.AddComponent(index, entity, new CalculatePathfindingFlag {
                    finalPositionEntity = chosen,
                    initialPositionEntity = startTileEntity
                });
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
        EntityQuery m_Group = GetEntityQuery(ComponentType.ReadOnly<CanMove>());
        NativeArray<Entity> mapBuffer = new NativeArray<Entity>(TileHandler.instance.height * TileHandler.instance.width, Allocator.TempJob);

        GetBufferArray getBufferArray = new GetBufferArray
        {
            output = mapBuffer
        };

        JobHandle bufferjob = getBufferArray.Run(this, inputDeps);

        int randomSeed = UnityEngine.Random.Range(0, m_Group.CalculateEntityCount() - 1);

        PrepareEntityToMove prepareJob = new PrepareEntityToMove
        {
            entityCommandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
            canMoveTiles = m_Group.ToEntityArray(Allocator.TempJob),
            mapEntityBuffer = mapBuffer,
            getComponentDataFromEntity = GetComponentDataFromEntity<Tile>(true),
            height = TileHandler.instance.height,
            width = TileHandler.instance.width,
            randomSeed = randomSeed
        };

        JobHandle jobHandle = prepareJob.Schedule(this, inputDeps);
        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(jobHandle);

        return jobHandle;

    }
}
