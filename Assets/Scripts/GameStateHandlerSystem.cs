using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;

public enum TurnOrder
{
    AITurn, Player1
}

public struct AwaitActionFlag : IComponentData { }

public struct ReadyToHandle : IComponentData { }

public struct AIComponent : IComponentData { }

public struct CurrentTurn : IComponentData { public TurnOrder turnOrder; }

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

//[UpdateAfter(typeof(InputSystem))]
public class GameStateHandlerSystem : ComponentSystem
{
    private DynamicBuffer<Entity> entitiesBuffer;
    private DynamicBuffer<Entity> playerBuffer;
    private DynamicBuffer<Entity> aiBuffer;

    private NativeArray<Entity> sortedArray;

    private TurnOrder currentTurn = TurnOrder.Player1;

    protected override void OnUpdate()
    {
        Entities.WithAll<CurrentTurn>().ForEach((Entity entity, ref CurrentTurn currentTurn) => {
            currentTurn.turnOrder = this.currentTurn;
        });

        Entities.ForEach((DynamicBuffer<EntityBuffer> buffer) =>
        {
            entitiesBuffer = buffer.Reinterpret<Entity>();
        });

        Entities.ForEach((DynamicBuffer<PlayerEntityBuffer> buffer) =>
        {
            playerBuffer = buffer.Reinterpret<Entity>();
        });

        Entities.ForEach((DynamicBuffer<AiBuffer> buffer) =>
        {
            aiBuffer = buffer.Reinterpret<Entity>();
        });

        Entities.WithAll<SSoldier>().ForEach((Entity entity) =>
        {
            sortedArray = entitiesBuffer.AsNativeArray();
            if (!sortedArray.Contains(entity))
            {
                entitiesBuffer.Add(entity);
            }
        });

        Entities.WithAll<AIComponent, SSoldier>().ForEach((Entity entity) =>
        {
            sortedArray = aiBuffer.AsNativeArray();
            if (!sortedArray.Contains(entity))
            {
                aiBuffer.Add(entity);
            }
        });

        Entities.WithNone<AIComponent>().WithAll<SSoldier>().ForEach((Entity entity) =>
        {
            sortedArray = playerBuffer.AsNativeArray();
            if (!sortedArray.Contains(entity))
            {
                playerBuffer.Add(entity);
            }
        });

        if(currentTurn == TurnOrder.Player1)
        {
            var selectedUnitCount = Entities.WithNone<AIComponent>().WithAll<AwaitActionFlag>().ToEntityQuery().CalculateEntityCount();
            if(selectedUnitCount == 0)
            {
                currentTurn = TurnOrder.AITurn;

                //DEBUG stuff
                Entities.WithNone<AIComponent>().WithAll<SSoldier>().ForEach((Entity entity, ref SSoldier soldier) =>
                {
                    PostUpdateCommands.AddComponent(entity, new AwaitActionFlag { });
                    soldier.Movement = 4;
                });
            }
        }

        if (currentTurn == TurnOrder.AITurn)
        {
            var selectedUnitCount = Entities.WithAll<AIComponent, AwaitActionFlag>().ToEntityQuery().CalculateEntityCount();
            if (selectedUnitCount == 0)
            {
                currentTurn = TurnOrder.Player1;

                //DEBUG Stuff
                Entities.WithAll<AIComponent, SSoldier>().ForEach((Entity entity, ref SSoldier soldier) =>
                {
                    PostUpdateCommands.AddComponent(entity, new AwaitActionFlag { });
                    soldier.Movement = 4;
                });
            }
        }

        //sortedArray = turnOrderBuffer.AsNativeArray();
        //sortedArray.Sort(new TurnOrdercomparer { entityManager = EntityManager });


        ////Entities.WithAll<AIComponent, SSoldier>().ForEach((Entity entity) => {
        ////    PostUpdateCommands.RemoveComponent<ReadyToHandle>(entity);
        ////});

        //var selectedUnitCount = Entities.WithAll<UnitSelected>().ToEntityQuery().CalculateEntityCount();
        //if (selectedUnitCount == 0 && turnOrderBuffer.Length >= 0)
        //{
        //    PostUpdateCommands.AddComponent<UnitSelected>(turnOrderBuffer[0]); 
        //}

        ////Query for waiting
        //var waiting = Entities.WithAll<AwaitActionFlag>().ToEntityQuery().CalculateEntityCount();
        //if (waiting == 0)
        //{
        //    //TODO - Query for all soldiers, should generalize this later with a more specialized component to designate actual game actors
        //    Entities.WithAll<AIComponent, SSoldier>().ForEach((Entity entity, ref SSoldier soldier) => {
        //        PostUpdateCommands.AddComponent<AwaitActionFlag>(entity);
        //        //Reset all the atributes to the desired values
        //        soldier.Movement = 4;
        //    });

        //    Entities.WithNone<AIComponent>().WithAll<SSoldier>().ForEach((Entity entity, ref SSoldier soldier) => {
        //        PostUpdateCommands.AddComponent<AwaitActionFlag>(entity);
        //        //Reset all the atributes to the desired values
        //        soldier.Movement = 4;
        //    });

        //}
    }
}
