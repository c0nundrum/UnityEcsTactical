using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;

[UpdateAfter(typeof(UnitMoveSystem))]
public class FinishedMoveSystem : JobComponentSystem
{
    [BurstCompile]
    private struct ResetJobSignal : IJobForEachWithEntity<UnitFinishedMove>
    {
        public NativeArray<Entity> output;

        public EntityCommandBuffer.Concurrent commandBuffer;

        public ComponentType unitFinishedMoveType;

        public void Execute(Entity entity, int index, [ReadOnly] ref UnitFinishedMove c0)
        {
            output[0] = entity;
            commandBuffer.RemoveComponent(index, entity, unitFinishedMoveType);
        }
    }

    [BurstCompile]
    private struct ResetBuffer : IJob
    {
        [DeallocateOnJobCompletion]
        public NativeArray<Entity> entity;

        public BufferFromEntity<PathBuffer> lookup;

        public void Execute()
        {
            if (!entity[0].Equals(Entity.Null))
            {
                lookup[entity[0]].Clear();
            }                
        }
    }

    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;
    private NativeArray<Entity> output;

    protected override void OnCreate()
    {
        endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        base.OnCreate();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {

        output = new NativeArray<Entity>(1, Allocator.TempJob);

        ResetJobSignal resetJobSignal = new ResetJobSignal
        {
            commandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
            unitFinishedMoveType = typeof(UnitFinishedMove),
            output = output
        };

        inputDeps = resetJobSignal.Schedule(this, inputDeps);

        ResetBuffer resetBufferJob = new ResetBuffer
        {
            entity = output,
            lookup = GetBufferFromEntity<PathBuffer>(false)
        };

        inputDeps = resetBufferJob.Schedule(inputDeps);

        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(inputDeps);

        return inputDeps;
    }
}
