using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Collections;

public class TileHandler : MonoBehaviour
{

    public static TileHandler instance;

    [SerializeField] private Mesh mesh;
    [SerializeField] private Material material;
    [SerializeField] private int width;
    [SerializeField] private int height;

    public Material tileSelectedMaterial;
    public Mesh tileSelectedMesh;

    private EntityManager entityManager;

    private void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {

        entityManager = World.Active.EntityManager;

        NativeArray<Entity> entityArray = new NativeArray<Entity>(width * height, Allocator.Temp);

        EntityArchetype entityArchetype = entityManager.CreateArchetype(
            typeof(Tile),
            typeof(RenderMesh),
            typeof(LocalToWorld),
            typeof(Translation),
            typeof(NonUniformScale)
        );


        entityManager.CreateEntity(entityArchetype, entityArray);

        int loopCounter = 0;
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                Entity entity = entityArray[loopCounter];
                entityManager.SetSharedComponentData(entity, new RenderMesh
                {
                    mesh = mesh,
                    material = material
                });

                entityManager.SetComponentData(entity, new Translation
                {
                    Value = new float3(1 * j, 1 * i, 0f)
                });

                entityManager.SetComponentData(entity, new NonUniformScale
                {
                    Value = new float3(1, 1, 1)
                });

                entityManager.SetComponentData(entity, new Tile
                {
                    walkable = true,
                    coordinates = new float2(j, i)
                });



                loopCounter++;
            }
        }   

    }
}
