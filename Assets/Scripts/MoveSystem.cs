using Unity.Entities;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

// Unit go to Move Position
[UpdateAfter(typeof(ActualMovementSystem))]
public class UnitMoveSystem : JobComponentSystem
{

    private struct Job : IJobForEachWithEntity<MoveTo, Translation, SSoldier>
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
                        //if (math.length(direction) > 0.001f)
                        {
                            translation.Value = Vector3.MoveTowards(translation.Value, moveTo.position, moveTo.moveSpeed * deltaTime);
                            //translation.Value = Vector3.MoveTowards(translation.Value, moveTo.position, 0.1f);
                            //direction = math.normalize(direction);
                            ////Debug.Log(direction);
                            //translation.Value = translation.Value + direction * moveTo.moveSpeed* deltaTime;
                            //////Far from target position, Move to position
                            ////float3 moveDir = math.normalize(moveTo.position - translation.Value);
                            ////moveTo.lastMoveDir = moveDir;
                            ////translation.Value += moveDir * moveTo.moveSpeed * deltaTime;
                            ////Debug.Log(translation.Value);

                        }
                        else
                        {
                            // Already there
                            //soldier.Movement -= moveTo.moveCost;
                            Debug.Log("Called short");
                            moveTo.positionInMove++;
                            moveTo.move = false;
                        }
                    } else
                    {
                        Debug.Log("Called long");
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
        Job job = new Job
        {
            deltaTime = Time.DeltaTime,
        };
        return job.Schedule(this, inputDeps);
    }

}
