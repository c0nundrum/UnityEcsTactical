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
            typeof(UnitSelected)
        );


        Entity entity = entityManager.CreateEntity(entityArchetype);

        entityManager.SetSharedComponentData(entity, new RenderMesh
        {
            mesh = mesh,
            material = material
        });

        entityManager.SetComponentData(entity, new MoveTo { move = true, position = float3.zero, moveSpeed = 10f });
        entityManager.SetComponentData(entity, new SSoldier {currentCoordinates = new float2(0,0), speed = 4 });

    }

}
