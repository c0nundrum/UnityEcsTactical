using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;

[UpdateAfter(typeof(GameStateHandlerSystem))]
public class InputSystem : ComponentSystem
{
    private DynamicBuffer<Entity> turnOrderBuffer;
    private Entity gameHandler;
    private CurrentTurn currentTurn;

    private DynamicBuffer<Entity> playerBuffer;
    private DynamicBuffer<Entity> aiBuffer;

    private int currentIndex = 0;

    protected override void OnUpdate()
    {
        Entities.WithAllReadOnly<CurrentTurn>().ForEach((Entity entity) => {        });

        Entities.WithAllReadOnly<CurrentTurn>().ForEach((Entity entity, ref CurrentTurn currentTurn) => {
            this.gameHandler = entity;
            this.currentTurn = currentTurn;
            this.playerBuffer = EntityManager.GetBuffer<PlayerEntityBuffer>(entity).Reinterpret<Entity>();
            this.aiBuffer = EntityManager.GetBuffer<AiBuffer>(entity).Reinterpret<Entity>();
        });

        var selectedUnitCount = Entities.WithAll<UnitSelected>().ToEntityQuery().CalculateEntityCount();
        if(selectedUnitCount == 0)
        {
            currentIndex = 0;
            //TODO - This assumes there are entities in the field
            if (currentTurn.turnOrder == TurnOrder.Player1 && playerBuffer.Length >= 0)
            {               
                Debug.Log("No unit selected, selecting player");
                PostUpdateCommands.AddComponent(playerBuffer[currentIndex], new UnitSelected { });
                PostUpdateCommands.AddComponent(playerBuffer[currentIndex], new CalculateMoveAreaFlag { });
            } else if (currentTurn.turnOrder == TurnOrder.AITurn && aiBuffer.Length >= 0)
            {
                Debug.Log("No unit selected, selecting AI");
                PostUpdateCommands.AddComponent(aiBuffer[0], new UnitSelected { });
                PostUpdateCommands.AddComponent(aiBuffer[0], new CalculateMoveAreaFlag { });
            }
        }

        //DEBUG - WithAny, AIComponents that receives readytohandle should not be controled by the player
        Entities.WithAll<UnitSelected>().WithAny<AwaitActionFlag, ReadyToHandle>().ForEach((Entity entity) => {
            //End turn
            if (Input.GetKeyDown("space"))
            {
                if (currentTurn.turnOrder == TurnOrder.Player1 && currentIndex < playerBuffer.Length - 1)
                {
                    PostUpdateCommands.AddComponent(playerBuffer[currentIndex + 1], new UnitSelected { });
                    PostUpdateCommands.AddComponent(playerBuffer[currentIndex + 1], new CalculateMoveAreaFlag { });
                    currentIndex++;
                }
                else if (currentTurn.turnOrder == TurnOrder.Player1 && currentIndex == playerBuffer.Length - 1)
                {
                    PostUpdateCommands.AddComponent(aiBuffer[0], new UnitSelected { });
                    PostUpdateCommands.AddComponent(aiBuffer[0], new CalculateMoveAreaFlag { });
                    PostUpdateCommands.SetComponent(gameHandler, new CurrentTurn { turnOrder = TurnOrder.AITurn });
                    currentIndex = 0;
                }
                           
                if(currentTurn.turnOrder == TurnOrder.AITurn)
                {
                    if (currentIndex < aiBuffer.Length - 1)
                    {
                        PostUpdateCommands.AddComponent(aiBuffer[currentIndex + 1], new UnitSelected { });
                        PostUpdateCommands.AddComponent(aiBuffer[currentIndex + 1], new CalculateMoveAreaFlag { });
                        currentIndex++;
                    }
                    else if (currentIndex == aiBuffer.Length - 1)
                    {
                        PostUpdateCommands.AddComponent(playerBuffer[0], new UnitSelected { });
                        PostUpdateCommands.AddComponent(playerBuffer[0], new CalculateMoveAreaFlag { });
                        PostUpdateCommands.SetComponent(gameHandler, new CurrentTurn { turnOrder = TurnOrder.Player1 });
                        currentIndex = 0;
                    }
                }
                

                //TODO - This is debug, should be moved to when the action ends
                if(EntityManager.HasComponent(entity, typeof(ReadyToHandle)))
                {
                    PostUpdateCommands.RemoveComponent<ReadyToHandle>(entity);
                }

                PostUpdateCommands.RemoveComponent<UnitSelected>(entity);
                PostUpdateCommands.RemoveComponent<AwaitActionFlag>(entity);
            }
        });

    }
}
