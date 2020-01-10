using Unity.Entities;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

// Unit go to Move Position
[UpdateAfter(typeof(ActualMovementSystem))]
public class UnitMoveSystem : JobComponentSystem
{

    private struct MovingJob : IJobForEachWithEntity<MoveTo, Translation, SSoldier>
    {

        public float deltaTime;

        public void Execute(Entity entity, int index, ref MoveTo moveTo, ref Translation translation, ref SSoldier soldier)
        {
            if (moveTo.longMove)
            {
                if (moveTo.move)
                {
                    if (math.distance(translation.Value, moveTo.finalDestination) > 0.001f)
                    {
                        var direction = moveTo.position - translation.Value;
                        if (math.distance(translation.Value, moveTo.position) > 0.001f)
                        {
                            translation.Value = Vector3.MoveTowards(translation.Value, moveTo.position, moveTo.moveSpeed * deltaTime);
                        }
                        else
                        {
                            // Already there
                            //soldier.Movement -= moveTo.moveCost;
                            Debug.Log("Called short");
                            soldier.Movement -= moveTo.moveCost;
                            moveTo.positionInMove++;
                            moveTo.move = false;
                        }
                    } else
                    {
                        Debug.Log("Called long");
                        soldier.Movement -= moveTo.moveCost;
                        moveTo.positionInMove++;
                        moveTo.longMove = false;
                        moveTo.move = false;
                    }
                }
            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        MovingJob job = new MovingJob
        {
            deltaTime = Time.DeltaTime,
        };
        return job.Schedule(this, inputDeps);
    }

}
