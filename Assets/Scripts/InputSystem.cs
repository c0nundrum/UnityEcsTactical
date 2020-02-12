using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Rendering;

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
                //PostUpdateCommands.AddComponent(playerBuffer[currentIndex], new CalculateMoveAreaFlag { });
            } else if (currentTurn.turnOrder == TurnOrder.AITurn && aiBuffer.Length >= 0)
            {
                Debug.Log("No unit selected, selecting AI");
                PostUpdateCommands.AddComponent(aiBuffer[0], new UnitSelected { });
                //PostUpdateCommands.AddComponent(aiBuffer[0], new CalculateMoveAreaFlag { });
            }
        }

        //DEBUG - WithAny, AIComponents that receives readytohandle should not be controled by the player
        Entities.WithAll<UnitSelected, Translation>().WithAny<AwaitActionFlag, ReadyToHandle>().ForEach((Entity entity, ref Translation translation) => {
            //End turn
            if (Input.GetKeyDown("space"))
            {
                if (currentTurn.turnOrder == TurnOrder.Player1 && currentIndex < playerBuffer.Length - 1)
                {
                    PostUpdateCommands.AddComponent(playerBuffer[currentIndex + 1], new UnitSelected { });
                    //PostUpdateCommands.AddComponent(playerBuffer[currentIndex + 1], new CalculateMoveAreaFlag { });
                    currentIndex++;
                }
                else if (currentTurn.turnOrder == TurnOrder.Player1 && currentIndex == playerBuffer.Length - 1)
                {
                    PostUpdateCommands.AddComponent(aiBuffer[0], new UnitSelected { });
                    //PostUpdateCommands.AddComponent(aiBuffer[0], new CalculateMoveAreaFlag { });
                    PostUpdateCommands.SetComponent(gameHandler, new CurrentTurn { turnOrder = TurnOrder.AITurn });
                    currentIndex = 0;
                }
                           
                if(currentTurn.turnOrder == TurnOrder.AITurn)
                {
                    if (currentIndex < aiBuffer.Length - 1)
                    {
                        PostUpdateCommands.AddComponent(aiBuffer[currentIndex + 1], new UnitSelected { });
                        //PostUpdateCommands.AddComponent(aiBuffer[currentIndex + 1], new CalculateMoveAreaFlag { });
                        currentIndex++;
                    }
                    else if (currentIndex == aiBuffer.Length - 1)
                    {
                        PostUpdateCommands.AddComponent(playerBuffer[0], new UnitSelected { });
                        //PostUpdateCommands.AddComponent(playerBuffer[0], new CalculateMoveAreaFlag { });
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

            //TODO - Screen Culling system concept
            //Debug, please delete this
            //Seems to work, at 1024 tiles, aproximately 1300km of ingame units we go from 7fps to over 250, we just need to store the actual tile info
            //somewhere and just display what is needed (comparatively we had 350 fps with 10x10 grid, seems everything is scaling well so far)
            //we should have a limit for copying the pathfinding data
            if (Input.GetKeyDown(KeyCode.P))
            {
                EntityQuery e_GroupMap = GetEntityQuery(typeof(MapEntityBuffer));
                NativeArray<Entity> e_array = e_GroupMap.ToEntityArray(Allocator.TempJob);

                DynamicBuffer<Entity> mapEntityBuffers = EntityManager.GetBuffer<MapEntityBuffer>(e_array[0]).Reinterpret<Entity>();
                for(int x = 0; x < TileHandler.instance.width; x++)
                {
                    for (int y = 0; y < TileHandler.instance.height; y++)
                    {
                        if (x > 10 || y > 10)
                        {
                            PostUpdateCommands.RemoveComponent(mapEntityBuffers[y * TileHandler.instance.width + x], typeof(RenderMesh));
                            PostUpdateCommands.RemoveComponent(mapEntityBuffers[y * TileHandler.instance.width + x], typeof(Transform));
                            PostUpdateCommands.RemoveComponent(mapEntityBuffers[y * TileHandler.instance.width + x], typeof(LocalToWorld));
                        }
                    }
                }
                e_array.Dispose();

            }

            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                float3 screenMousePosition = Input.mousePosition;
                float3 worldMousePosition = Camera.main.ScreenToWorldPoint(screenMousePosition);

                //TODO - Make this ternary for broadcasting on the threads
                int2 destination;
                if (math.frac(worldMousePosition.x) > 0.5)
                    destination.x = (int)math.ceil(worldMousePosition.x);
                else
                    destination.x = (int)math.floor(worldMousePosition.x);

                if (math.frac(worldMousePosition.y) > 0.5)
                    destination.y = (int)math.ceil(worldMousePosition.y);
                else
                    destination.y = (int)math.floor(worldMousePosition.y);

                int2 startPosition;
                if (math.frac(worldMousePosition.x) > 0.5)
                    startPosition.x = (int)math.ceil(translation.Value.x);
                else
                    startPosition.x = (int)math.floor(translation.Value.x);

                if (math.frac(worldMousePosition.y) > 0.5)
                    startPosition.y = (int)math.ceil(translation.Value.y);
                else
                    startPosition.y = (int)math.floor(translation.Value.y);

                PostUpdateCommands.AddComponent(entity, new CalculateMove
                {
                    Destination = destination,
                    StartPosition = startPosition
                });

            }

        });

    }
}
