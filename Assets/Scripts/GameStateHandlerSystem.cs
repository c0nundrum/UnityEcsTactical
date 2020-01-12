using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;

public struct AwaitActionFlag : IComponentData { }

public struct ReadyToHandle : IComponentData { }

public struct AIComponent : IComponentData { }

public struct TurnOrdercomparer : IComparer<Entity>
{
    public EntityManager entityManager;

    public int Compare(Entity x, Entity y)
    {
        SSoldier soldierX = entityManager.GetComponentData<SSoldier>(x);
        SSoldier soldierY = entityManager.GetComponentData<SSoldier>(y);

        return soldierX.Initiative.CompareTo(soldierY.Initiative);
    }
}

public class GameStateHandlerSystem : ComponentSystem
{
    private DynamicBuffer<Entity> turnOrderBuffer;
    private NativeArray<Entity> sortedArray;

    private int currentTurn = 0;

    protected override void OnUpdate()
    {

        Entities.WithAll<SSoldier>().ForEach((Entity entity) => {     });

        Entities.ForEach((DynamicBuffer<EntityBuffer> buffer) =>
        {
            turnOrderBuffer = buffer.Reinterpret<Entity>();
        });

        sortedArray = turnOrderBuffer.AsNativeArray();

        Entities.WithAll<SSoldier>().ForEach((Entity entity) => {
            sortedArray = turnOrderBuffer.AsNativeArray();
            if (!sortedArray.Contains(entity))
            {
                turnOrderBuffer.Add(entity);
            }
        });

        sortedArray = turnOrderBuffer.AsNativeArray();
        sortedArray.Sort(new TurnOrdercomparer { entityManager = EntityManager });
        

        Entities.WithAll<AIComponent, SSoldier>().ForEach((Entity entity) => {
            PostUpdateCommands.RemoveComponent<ReadyToHandle>(entity);
        });

        //Query for waiting
        var waiting = Entities.WithAll<AwaitActionFlag>().ToEntityQuery().CalculateEntityCount();
        if (waiting == 0)
        {
            //TODO - Query for all soldiers, should generalize this later with a more specialized component to designate actual game actors
            Entities.WithAll<AIComponent, SSoldier>().ForEach((Entity entity) => {
                PostUpdateCommands.AddComponent<ReadyToHandle>(entity);
            });

            Entities.WithNone<AIComponent>().WithAll<SSoldier>().ForEach((Entity entity, ref SSoldier soldier) => {
                PostUpdateCommands.AddComponent<AwaitActionFlag>(entity);
                //Reset all the atributes to the desired values
                soldier.Movement = 4;
            });

        }
    }
}
