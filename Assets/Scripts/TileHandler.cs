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
            typeof(NonUniformScale),
            typeof(NeighbourTiles)
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
                    coordinates = new float2(j, i),
                    ownerEntity = entity
                });

                loopCounter++;
            }
        }

        //Setup Neighbour components
        for (int i = 0; i < width * height; i++)
        {
            Entity entity = entityArray[i];
            Tile tile = entityManager.GetComponentData<Tile>(entity);

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

    }
}
