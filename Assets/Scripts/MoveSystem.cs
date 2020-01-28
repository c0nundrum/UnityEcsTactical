using Unity.Entities;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;

[UpdateAfter(typeof(PathfindingSystem))]
public class UnitMoveSystem : JobComponentSystem
{

    [BurstCompile]
    private struct MovingJob : IJobForEachWithEntity<MovePath, Translation, SSoldier>
    {
        public EntityCommandBuffer.Concurrent commandBuffer;

        [ReadOnly]
        public BufferFromEntity<PathBuffer> lookup;

        [ReadOnly]
        public ComponentType componentType;

        public float deltaTime;

        public void Execute(Entity entity, int index, ref MovePath movePath, ref Translation translation, ref SSoldier soldier)
        {
            var buffer = lookup[entity].Reinterpret<int2>();
            if (movePath.positionInMove >= 0)
            {
                if (math.distance(new float2(translation.Value.x, translation.Value.y), buffer[movePath.positionInMove]) > 0.001f)
                {
                    var direction = new float3(buffer[movePath.positionInMove], 0) - translation.Value;
                    translation.Value = Vector3.MoveTowards(translation.Value, new float3(buffer[movePath.positionInMove], 0), movePath.moveSpeed * deltaTime);
                }
                else
                {
                    // Already there
                    movePath.positionInMove--;                   
                }
            } else
            {
                commandBuffer.RemoveComponent(index, entity, componentType);
                commandBuffer.AddComponent(index, entity, new UnitFinishedMove { });

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

        MovingJob job = new MovingJob
        {
            componentType = typeof(MovePath),
            lookup = GetBufferFromEntity<PathBuffer>(true),
            commandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
            deltaTime = Time.DeltaTime,
        };

        inputDeps = job.Schedule(this, inputDeps);      

        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(inputDeps);

        return inputDeps;
    }

}
