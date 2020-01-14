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
    [SerializeField] private Material StoneMaterial;

    public int width;
    public int height;

    public Material tileSelectedMaterial;
    public Mesh tileSelectedMesh;

    public Material playerMaterial;
    public Material enemyMaterial;

    private EntityManager entityManager;

    private void Awake()
    {
        instance = this;
    }

    private void createGrass(Entity entity, int j, int i)
    {
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
            MovementCost = 1
        });

        entityManager.SetComponentData(entity, new PathfindingComponent
        {
            isPath = false,
            coordinates = new float2(j, i),
            gCost = int.MaxValue,
            hCost = 0
        });
    }

    private void createStone(Entity entity, int j, int i)
    {
        entityManager.SetSharedComponentData(entity, new RenderMesh
        {
            mesh = mesh,
            material = StoneMaterial
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
            MovementCost = 1
        });

        entityManager.SetComponentData(entity, new PathfindingComponent
        {
            isPath = false,
            coordinates = new float2(j, i),
            gCost = int.MaxValue,
            hCost = 99
        });
    }

    // Start is called before the first frame update
    void Start()
    {

        entityManager = World.Active.EntityManager;

        NativeArray<Entity> entityArray = new NativeArray<Entity>(width * height, Allocator.Temp);
        NativeArray<Entity> entityArrayStone = new NativeArray<Entity>(width * height, Allocator.Temp);
        NativeArray<Entity> entityArrayGrass = new NativeArray<Entity>(width * height, Allocator.Temp);

        EntityArchetype entityArchetype = entityManager.CreateArchetype(
            typeof(Tile),
            typeof(RenderMesh),
            typeof(LocalToWorld),
            typeof(Translation),
            typeof(NonUniformScale),
            typeof(NeighbourTiles),
            typeof(PathfindingComponent)
        );
        EntityArchetype entityArchetype02 = entityManager.CreateArchetype(
            typeof(Tile),
            typeof(RenderMesh),
            typeof(LocalToWorld),
            typeof(Translation),
            typeof(NonUniformScale),
            typeof(NeighbourTiles),
            typeof(PathfindingComponent)
        );


        //entityManager.CreateEntity(entityArchetype, entityArray);
        entityManager.CreateEntity(entityArchetype, entityArrayStone);
        entityManager.CreateEntity(entityArchetype02, entityArrayGrass);

        Entity e = entityManager.CreateEntity(typeof(MapBuffer));
        Entity f = entityManager.CreateEntity(typeof(MapEntityBuffer));

        

        int loopCounter = 0;
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                //Entity entity = entityArray[loopCounter];                
                Entity entity;                

                int diceRoll = (int) math.floor(UnityEngine.Random.Range(0, 5));
                //Debug.Log(diceRoll);
                if (diceRoll <= 2 || i == 0 && j == 0)
                {
                    entity = entityArrayGrass[loopCounter];
                    //entity = entityManager.CreateEntity(entityArchetype02);
                    createGrass(entity, j, i);
                } else
                {
                    entity = entityArrayStone[loopCounter];
                    //entity = entity = entityManager.CreateEntity(entityArchetype);
                    createStone(entity, j, i);
                }
                entityArray[loopCounter] = entity;
                loopCounter++;
            }
        }

        //Setup Neighbour components
        for (int i = 0; i < width * height; i++)
        {
            Entity entity = entityArray[i];
            Tile tile = entityManager.GetComponentData<Tile>(entity);

            //Bug, need to add it every iteration or it gets deallocated
            DynamicBuffer<MapBuffer> bufferFromEntity = entityManager.GetBuffer<MapBuffer>(e);
            DynamicBuffer<Tile> tileBuffer = bufferFromEntity.Reinterpret<Tile>();
            tileBuffer.Add(tile);
            DynamicBuffer<MapEntityBuffer> entityBufferFromEntity = entityManager.GetBuffer<MapEntityBuffer>(f);
            DynamicBuffer<Entity> entityBuffer = entityBufferFromEntity.Reinterpret<Entity>();
            entityBuffer.Add(entity);

            if (i == 0) // First Tile
            {
                //Debug.Log("First Tile");
                entityManager.SetComponentData(entity, new NeighbourTiles
                {
                    eTile = entityManager.GetComponentData<Tile>(entityArray[i + 1]),
                    //eTileEntity = entityArray[i + 1],
                    nTile = entityManager.GetComponentData<Tile>(entityArray[i + width]),
                    //nTileEntity = entityArray[i + width],
                });
            }
            else if (i < width - 1) // First Line
            {
                //Debug.Log("First Line");
                entityManager.SetComponentData(entity, new NeighbourTiles
                {
                    eTile = entityManager.GetComponentData<Tile>(entityArray[i + 1]),
                    //eTileEntity = entityArray[i + 1],
                    nTile = entityManager.GetComponentData<Tile>(entityArray[i + width]),
                    //nTileEntity = entityArray[i + width],
                    wTile = entityManager.GetComponentData<Tile>(entityArray[i - 1]),
                    //wTileEntity = entityArray[i - 1]
                });
            }
            else if (i == width - 1) // End of first Line
            {
                //Debug.Log("End of first Line");
                entityManager.SetComponentData(entity, new NeighbourTiles
                {
                    nTile = entityManager.GetComponentData<Tile>(entityArray[i + width]),
                    //nTileEntity = entityArray[i + width],
                    wTile = entityManager.GetComponentData<Tile>(entityArray[i - 1]),
                    //wTileEntity = entityArray[i - 1]
                });
            }
            else if (i % width == 0 && i / height < height - 1) // First Row, not last line
            {
                //Debug.Log("First Row");
                entityManager.SetComponentData(entity, new NeighbourTiles
                {
                    eTile = entityManager.GetComponentData<Tile>(entityArray[i + 1]),
                    //eTileEntity = entityArray[i + 1],
                    nTile = entityManager.GetComponentData<Tile>(entityArray[i + width]),
                    //nTileEntity = entityArray[i + width],
                    sTile = entityManager.GetComponentData<Tile>(entityArray[i - width]),
                    //sTileEntity = entityArray[i - width]
                });
            }
            else if (i % width == width - 1 && i / height < height - 1) // Last Row, not last line
            {
                //Debug.Log("Last Row");
                entityManager.SetComponentData(entity, new NeighbourTiles
                {
                    wTile = entityManager.GetComponentData<Tile>(entityArray[i - 1]),
                    //wTileEntity = entityArray[i - 1],
                    nTile = entityManager.GetComponentData<Tile>(entityArray[i + width]),
                    //nTileEntity = entityArray[i + width],
                    sTile = entityManager.GetComponentData<Tile>(entityArray[i - width]),
                    //sTileEntity = entityArray[i - width]
                });
            }
            else if (i % width == 0 && i / height == height - 1) // First Row Last Line
            {
                //Debug.Log("First Row Last Line");
                entityManager.SetComponentData(entity, new NeighbourTiles
                {
                    eTile = entityManager.GetComponentData<Tile>(entityArray[i + 1]),
                    //eTileEntity = entityArray[i + 1],
                    sTile = entityManager.GetComponentData<Tile>(entityArray[i - width]),
                    //sTileEntity = entityArray[i - width]
                });
            }
            else if (i == (width * height) - 1) // Last Tile
            {
                //Debug.Log("Last Tile");
                entityManager.SetComponentData(entity, new NeighbourTiles
                {
                    wTile = entityManager.GetComponentData<Tile>(entityArray[i - 1]),
                    //wTileEntity = entityArray[i - 1],
                    sTile = entityManager.GetComponentData<Tile>(entityArray[i - width]),
                    //sTileEntity = entityArray[i - width]
                });
            }
            else if (i / height == height - 1) // Last Line
            {
                //Debug.Log("Last Line");
                entityManager.SetComponentData(entity, new NeighbourTiles
                {
                    wTile = entityManager.GetComponentData<Tile>(entityArray[i - 1]),
                    //wTileEntity = entityArray[i - 1],
                    eTile = entityManager.GetComponentData<Tile>(entityArray[i + 1]),
                    //eTileEntity = entityArray[i + 1],
                    sTile = entityManager.GetComponentData<Tile>(entityArray[i - width]),
                    //sTileEntity = entityArray[i - width]
                });
            }
            else
            {
                //Debug.Log("Everything Else");
                entityManager.SetComponentData(entity, new NeighbourTiles
                {
                    wTile = entityManager.GetComponentData<Tile>(entityArray[i - 1]),
                    //wTileEntity = entityArray[i - 1],
                    eTile = entityManager.GetComponentData<Tile>(entityArray[i + 1]),
                    //eTileEntity = entityArray[i + 1],
                    sTile = entityManager.GetComponentData<Tile>(entityArray[i - width]),
                    //sTileEntity = entityArray[i - width],
                    nTile = entityManager.GetComponentData<Tile>(entityArray[i + width]),
                    //nTileEntity = entityArray[i + width],
                });
            }

        }

        SpawnerComponent spawnerComponent = new SpawnerComponent(entityArray);

    }
}
