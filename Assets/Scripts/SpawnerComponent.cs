using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

public class SpawnerComponent 
{

    private NativeArray<Entity> arrayOfEntities;
    private EntityManager entityManager;
    private EntityArchetype playerArchetype;
    private EntityArchetype aiArchetype;

    public SpawnerComponent(NativeArray<Entity> arrayOfEntities)
    {
        this.entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        this.playerArchetype = createPlayerArchetype();
        this.aiArchetype = createAIArchetype();
        this.arrayOfEntities = arrayOfEntities;
        this.spawnActors();
    }


    private void spawnActors()
    {
        //Creates the entity buffer
        Entity bufferEntity = entityManager.CreateEntity(typeof(EntityBuffer));
        entityManager.AddBuffer<AiBuffer>(bufferEntity);
        entityManager.AddBuffer<PlayerEntityBuffer>(bufferEntity);
        entityManager.AddComponentData(bufferEntity, new CurrentTurn { turnOrder = TurnOrder.Player1 });

        Entity e = spawnEntity(playerArchetype);
        setUpEntity(e, TileHandler.instance.tileSelectedMesh, TileHandler.instance.playerMaterial, new float3(0f, 0f, 0f), 20);
        entityManager.AddBuffer<PathBuffer>(e);

        e = spawnEntity(playerArchetype);
        setUpEntity(e, TileHandler.instance.tileSelectedMesh, TileHandler.instance.playerMaterial, validCoordinates(), 20);
        entityManager.AddBuffer<PathBuffer>(e);

        //Entity f = spawnEntity(playerArchetype);
        //setUpEntity(f, TileHandler.instance.tileSelectedMesh, TileHandler.instance.playerMaterial, new float3(3f, 3f, 0f), 20);

        Entity f = spawnEntity(aiArchetype);
        setUpEntity(f, TileHandler.instance.tileSelectedMesh, TileHandler.instance.enemyMaterial, validCoordinates(), 30);
        entityManager.AddBuffer<PathBuffer>(f);

        f = spawnEntity(aiArchetype);
        setUpEntity(f, TileHandler.instance.tileSelectedMesh, TileHandler.instance.enemyMaterial, validCoordinates(), 30);
        entityManager.AddBuffer<PathBuffer>(f);

    }

    private float3 validCoordinates()
    {
        int x = (int)math.floor(UnityEngine.Random.Range(0, TileHandler.instance.width - 1));
        int y = (int)math.floor(UnityEngine.Random.Range(0, TileHandler.instance.height - 1));

        bool found = false;
        while (!found)
        {
            Entity tileEntity = arrayOfEntities[y * TileHandler.instance.width + x];
            Tile tile = entityManager.GetComponentData<Tile>(tileEntity);

            if (tile.walkable)
            {
                found = true;
            } else
            {
                if (x > 0 && y > 0 && x < TileHandler.instance.width - 1 && y < TileHandler.instance.height - 1)
                {
                    x = (int)math.floor(UnityEngine.Random.Range(x - 1, x + 1));
                    y = (int)math.floor(UnityEngine.Random.Range(y - 1, y + 1));
                } else if(x == 0 && y == 0  )
                {
                    x = (int)math.floor(UnityEngine.Random.Range(0, TileHandler.instance.width - 1));
                    y = (int)math.floor(UnityEngine.Random.Range(0, TileHandler.instance.height - 1));
                } else
                {
                    x = (int)math.floor(UnityEngine.Random.Range(0, TileHandler.instance.width - 1));
                    y = (int)math.floor(UnityEngine.Random.Range(0, TileHandler.instance.height - 1));
                }
            }
        }
        return new float3(x, y, 0f);
    }

    private Entity spawnEntity(EntityArchetype archetype)
    {
        return entityManager.CreateEntity(archetype);
    }

    private void setUpEntity(Entity entity, Mesh mesh, Material material, float3 coordinates, int initiative)
    {
        entityManager.SetSharedComponentData(entity, new RenderMesh
        {
            mesh = mesh,
            material = material
        });

        //entityManager.SetComponentData(entity, new MoveTo { move = false, position = float3.zero, moveSpeed = 5f });
        entityManager.SetComponentData(entity, new Translation { Value = coordinates });
        entityManager.AddComponentData(entity, new AwaitActionFlag { });
        entityManager.SetComponentData(entity, new SSoldier { currentCoordinates = new int2((int)math.floor(coordinates.x), (int)math.floor(coordinates.y)), Movement = 4, Initiative = initiative });
        entityManager.SetComponentData(entity, new Scale { Value = 1f });
    }

    private EntityArchetype createPlayerArchetype()
    {
        return entityManager.CreateArchetype(
            typeof(SSoldier),
            typeof(RenderMesh),
            //typeof(MovePath),
            typeof(LocalToWorld),
            typeof(Translation),
            typeof(Scale));
    }

    //private EntityArchetype createMouseSingleton()
    //{
    //    return entityManager.CreateArchetype(
    //        typeof(RenderMesh),
    //        typeof(LocalToWorld),
    //        typeof(Translation),
    //        typeof(MouseCursor));
    //}

    private EntityArchetype createAIArchetype()
    {
        return entityManager.CreateArchetype(
            typeof(SSoldier),
            typeof(RenderMesh),
            //typeof(MovePath),
            typeof(LocalToWorld),
            typeof(AIComponent),
            typeof(Translation),
            typeof(Scale));
    }

}
