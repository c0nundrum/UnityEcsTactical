using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;

[UpdateAfter(typeof(GameStateHandlerSystem))]
public class InputSystem : ComponentSystem
{
    private DynamicBuffer<Entity> turnOrderBuffer;

    protected override void OnUpdate()
    {

        //DEBUG - WithAny, AIComponents that receives readytohandle should not be controled by the player
        Entities.WithAll<UnitSelected>().WithAny<AwaitActionFlag, ReadyToHandle>().ForEach((Entity entity) => {
            //End turn
            if (Input.GetKeyDown("space"))
            {
                PostUpdateCommands.RemoveComponent<UnitSelected>(entity);

                Entities.ForEach((DynamicBuffer<EntityBuffer> buffer) =>
                {
                    turnOrderBuffer = buffer.Reinterpret<Entity>();
                    var turnOrderArray = turnOrderBuffer.AsNativeArray();
                    for (int i = 0; i < turnOrderBuffer.Length; i++)
                    {
                        if (turnOrderBuffer[i].Equals(entity))
                        {
                            if (i == turnOrderBuffer.Length - 1)
                            {
                                PostUpdateCommands.AddComponent<UnitSelected>(turnOrderBuffer[0]);
                            }
                            else
                            {
                                PostUpdateCommands.AddComponent<UnitSelected>(turnOrderBuffer[i + 1]);
                            }
                            break;
                        }
                    }
                });

                PostUpdateCommands.RemoveComponent<AwaitActionFlag>(entity);

                //TODO - This is debug, should be moved to when the action ends
                if(EntityManager.HasComponent(entity, typeof(ReadyToHandle)))
                {
                    PostUpdateCommands.RemoveComponent<ReadyToHandle>(entity);
                }
            }
        });

    }
}
