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

    private MapGeneratorComponent mapGen;

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


    // Start is called before the first frame update
    void Start()
    {
        
        mapGen = new MapGeneratorComponent(width, height);

        char[] map = mapGen.GetMap();

        entityManager = World.Active.EntityManager;

        MapTranslationComponent mapTranslationComponent = new MapTranslationComponent(map, entityManager, mesh, StoneMaterial, material, width, height);

        NativeArray<Entity> entityArray = mapTranslationComponent.GetEntityArray();

        Entity e = entityManager.CreateEntity(typeof(MapBuffer));
        Entity f = entityManager.CreateEntity(typeof(MapEntityBuffer));
     

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

            NeighbourTiles neighbours = new NeighbourTiles {
                eTile = Entity.Null,
                neTile = Entity.Null,
                seTile = Entity.Null,
                nTile = Entity.Null,
                nwTile = Entity.Null,
                swTile = Entity.Null,
                sTile = Entity.Null,
                wTile = Entity.Null
            };

            if (tile.coordinates.x - 1 >= 0)
            {
                neighbours.wTile = entityArray[(int)math.floor((tile.coordinates.y) * width + (tile.coordinates.x - 1))];
                if (tile.coordinates.y - 1 >= 0)
                    neighbours.swTile = entityArray[(int)math.floor((tile.coordinates.y - 1) * width + tile.coordinates.x - 1)];
                if (tile.coordinates.y + 1 < height)
                    neighbours.nwTile = entityArray[(int)math.floor((tile.coordinates.y + 1) * width + tile.coordinates.x - 1)];
            }
            if (tile.coordinates.x + 1 < width)
            {

                neighbours.eTile = entityArray[(int)math.floor((tile.coordinates.y) * width + (tile.coordinates.x + 1))];
                if (tile.coordinates.y - 1 >= 0)
                    neighbours.seTile = entityArray[(int)math.floor((tile.coordinates.y - 1) * width + tile.coordinates.x + 1)];
                if (tile.coordinates.y + 1 < height)
                    neighbours.neTile = entityArray[(int)math.floor((tile.coordinates.y + 1) * width + tile.coordinates.x + 1)];
            }
            //Down
            if (tile.coordinates.y - 1 >= 0)
                neighbours.sTile = entityArray[(int)math.floor((tile.coordinates.y - 1) * width + tile.coordinates.x)];

            //Up
            if (tile.coordinates.y + 1 < height)
                neighbours.nTile = entityArray[(int)math.floor((tile.coordinates.y + 1) * width + tile.coordinates.x)];

            entityManager.SetComponentData(entity, neighbours);

        }

        SpawnerComponent spawnerComponent = new SpawnerComponent(entityArray);

    }
}
