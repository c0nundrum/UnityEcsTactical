using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Mathematics;


public class MapTranslationComponent
{
    private char[] map;

    private Dictionary<char, NativeArray<Entity>> nativeArrayDict = new Dictionary<char, NativeArray<Entity>>();
    private Dictionary<char, EntityArchetype> entityArchetypeDict = new Dictionary<char, EntityArchetype>();
    private NativeArray<Entity> entityArray;

    private EntityManager entityManager;

    private readonly int mapWidth;
    private readonly int mapHeight;

    //TODO - this should be given another manner to load
    private Mesh mesh;
    private Material stoneMaterial;
    private Material material;

    public MapTranslationComponent(char[] map, EntityManager entityManager, Mesh mesh, Material stoneMaterial, Material material, int mapWidth, int mapHeight)
    {
        this.map = map;
        this.entityManager = entityManager;
        this.mesh = mesh;
        this.stoneMaterial = stoneMaterial;
        this.material = material;
        this.mapWidth = mapWidth;
        this.mapHeight = mapHeight;

        BuildArchetypeDict();
        GetArrayOfEntitiesList();
        BuildEntities();
        SetupComponents();
    }

    private void BuildArchetypeDict()
    {
        //TODO - move to a JSON based architecture
        //Grass
        entityArchetypeDict.Add('.', entityManager.CreateArchetype(
            typeof(Tile),
            typeof(RenderMesh),
            typeof(LocalToWorld),
            typeof(Translation),
            typeof(NonUniformScale),
            typeof(NeighbourTiles),
            typeof(PathfindingComponent)
        ));

        entityArchetypeDict.Add('#', entityManager.CreateArchetype(
            typeof(Tile),
            typeof(RenderMesh),
            typeof(LocalToWorld),
            typeof(Translation),
            typeof(NonUniformScale),
            typeof(NeighbourTiles),
            typeof(PathfindingComponent)
        ));
    }

    private void GetArrayOfEntitiesList()
    {
        var groups = map.GroupBy(v => v);

        foreach (var group in groups)
        {
            nativeArrayDict.Add(group.Key, new NativeArray<Entity>(group.Count(), Allocator.Temp));
        }

    }

    private void BuildEntities()
    {
        foreach (var dict in nativeArrayDict)
        {
            entityManager.CreateEntity(TranslateArchetype(dict.Key), dict.Value);
        }
    }

    private EntityArchetype TranslateArchetype(char c)
    {
        entityArchetypeDict.TryGetValue(c, out EntityArchetype value);
        return value;
    }


    private void SetupComponents()
    {
        entityArray = new NativeArray<Entity>(map.Length, Allocator.Temp);

        Dictionary<char, int> countersDict = new Dictionary<char, int>();

        foreach (var dict in nativeArrayDict)
        {
            countersDict.Add(dict.Key, 0);
        }

        for (int i = 0; i < map.Length; i++)
        {
            if(nativeArrayDict.TryGetValue(map[i], out NativeArray<Entity> value))
            {
                Entity entity;
                if (countersDict.TryGetValue(map[i], out int counter))
                {
                    entity = value[counter];
                    countersDict[map[i]]++;
                    TranslateComponent(entity, map[i], i);
                    entityArray[i] = entity;
                }
            }

        }
    }

    private void TranslateComponent(Entity en, char c, int loopCounter)
    {
        if (c == '.') CreateGrass(en, loopCounter);
        if (c == '#') CreateStone(en, loopCounter);
    }

    private void CreateStone(Entity entity, int loopCounter)
    {
        int j = loopCounter % mapWidth;
        int i = loopCounter / mapWidth;
        entityManager.SetSharedComponentData(entity, new RenderMesh
        {
            mesh = mesh,
            material = stoneMaterial
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
            walkable = false,
            coordinates = new float2(j, i),
            ownerEntity = entity,
            MovementCost = 99
        });

        entityManager.SetComponentData(entity, new PathfindingComponent
        {
            isPath = false,
            coordinates = new float2(j, i),
            gCost = int.MaxValue,
            hCost = 99
        });
    }

    private void CreateGrass(Entity entity, int loopCounter)
    {
        int j = loopCounter % mapWidth;
        int i = loopCounter / mapWidth;

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
            coordinates = new float2(j, i),
            ownerEntity = entity,
            //MovementCost = 1
            MovementCost = (int)math.floor(UnityEngine.Random.Range(1, 3))
        });

        entityManager.SetComponentData(entity, new PathfindingComponent
        {
            isPath = false,
            coordinates = new float2(j, i),
            gCost = int.MaxValue,
            hCost = 0
        });
    }

    public NativeArray<Entity> GetEntityArray()
    {
        return entityArray;
    }

}
