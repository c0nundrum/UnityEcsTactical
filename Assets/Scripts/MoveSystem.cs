using Unity.Entities;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

// Unit go to Move Position
public class UnitMoveSystem : JobComponentSystem
{

    private struct Job : IJobForEachWithEntity<MoveTo, Translation>
    {

        public float deltaTime;

        public void Execute(Entity entity, int index, ref MoveTo moveTo, ref Translation translation)
        {
            if (moveTo.move)
            {
                float reachedPositionDistance = 0.1f;
                if (math.distance(translation.Value, moveTo.position) > reachedPositionDistance)
                {
                    // Far from target position, Move to position
                    float3 moveDir = math.normalize(moveTo.position - translation.Value);
                    moveTo.lastMoveDir = moveDir;
                    translation.Value += moveDir * moveTo.moveSpeed * deltaTime;
                    //Debug.Log(translation.Value);
                }
                else
                {
                    // Already there
                    moveTo.move = false;
                }
            }
        }

    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        Job job = new Job
        {
            deltaTime = Time.DeltaTime,
        };
        return job.Schedule(this, inputDeps);
    }

}
