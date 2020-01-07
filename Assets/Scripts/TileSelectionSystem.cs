using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public struct HoverTile : IComponentData { }

//TODO - should be updated to use the DynamicBuffer for the mapping coordinates!
public class TileSelectionSystem : ComponentSystem
{
    
    protected override void OnUpdate()
    {
        float3 screenMousePosition = Input.mousePosition;
        float3 worldMousePosition = Camera.main.ScreenToWorldPoint(screenMousePosition);

        Entities.WithAll<HoverTile, Tile, Translation>().ForEach((Entity entity, ref Tile tile, ref Translation translation) => {
            PostUpdateCommands.RemoveComponent(entity, typeof(HoverTile));
        });

        Entities.WithAll<Tile, Translation>().ForEach((Entity entity, ref Translation translation, ref Tile tile) =>
        {
            float3 entityPosition = translation.Value;
            
            if(entityPosition.x - 0.5 < worldMousePosition.x && entityPosition.x + 0.5 > worldMousePosition.x && entityPosition.y - 0.5 < worldMousePosition.y && entityPosition.y + 0.5 > worldMousePosition.y)
            {
                PostUpdateCommands.AddComponent(entity, new HoverTile { });
                //Debug.Log(entityPosition);
                Graphics.DrawMesh(
                       TileHandler.instance.tileSelectedMesh,
                       entityPosition,
                       Quaternion.identity,
                       TileHandler.instance.tileSelectedMaterial,
                       0
                   );
            }
        });

    }

}
