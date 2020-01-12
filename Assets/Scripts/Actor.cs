using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

public class Actor : MonoBehaviour
{
    [SerializeField] private Mesh mesh;
    [SerializeField] private Material material;

    private EntityManager entityManager;

    // Start is called before the first frame update
    void Start()
    {

        entityManager = World.Active.EntityManager;

        EntityArchetype entityArchetype = entityManager.CreateArchetype(
            typeof(SSoldier),
            typeof(RenderMesh),
            typeof(MoveTo),
            typeof(LocalToWorld),
            typeof(Translation),
            //typeof(UnitSelected),
            typeof(Scale)
        );


        Entity e = entityManager.CreateEntity(typeof(EntityBuffer));
        Entity entity = entityManager.CreateEntity(entityArchetype);

        Entity entity02 = entityManager.CreateEntity(entityArchetype);
        Entity entity03 = entityManager.CreateEntity(entityArchetype);

        entityManager.SetSharedComponentData(entity, new RenderMesh
        {
            mesh = mesh,
            material = material
        });

        entityManager.SetSharedComponentData(entity02, new RenderMesh
        {
            mesh = mesh,
            material = material
        });

        entityManager.SetSharedComponentData(entity03, new RenderMesh
        {
            mesh = mesh,
            material = material
        });

        entityManager.SetComponentData(entity, new MoveTo { move = true, position = float3.zero, moveSpeed = 5f });
        entityManager.AddComponentData(entity, new UnitSelected { });
        entityManager.AddComponentData(entity, new AwaitActionFlag { });
        entityManager.SetComponentData(entity, new SSoldier {currentCoordinates = new float2(0,0), Movement = 4, Initiative = 10 });
        entityManager.SetComponentData(entity, new Scale { Value = 1f });

        entityManager.SetComponentData(entity02, new MoveTo { move = true, position = float3.zero, moveSpeed = 5f });
        entityManager.SetComponentData(entity02, new Translation { Value = new float3(3f, 3f, 0f) });
        entityManager.AddComponentData(entity02, new AwaitActionFlag { });
        entityManager.SetComponentData(entity02, new SSoldier { currentCoordinates = new float2(3f, 3f), Movement = 4, Initiative = 20 });
        entityManager.SetComponentData(entity02, new Scale { Value = 1f });

        entityManager.SetComponentData(entity03, new MoveTo { move = true, position = float3.zero, moveSpeed = 5f });
        entityManager.SetComponentData(entity03, new Translation { Value = new float3(4f, 4f, 0f) });
        entityManager.AddComponentData(entity03, new AwaitActionFlag { });
        entityManager.SetComponentData(entity03, new SSoldier { currentCoordinates = new float2(4f, 4f), Movement = 4, Initiative = 30 });
        entityManager.SetComponentData(entity03, new Scale { Value = 1f });


    }

}
