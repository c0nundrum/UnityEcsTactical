using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGeneratorComponent
{

    private char[] map;

    private readonly int mapWidth;
    private readonly int mapHeight;

    public MapGeneratorComponent(int mapWidth, int mapHeight)
    {
        this.mapWidth = mapWidth;
        this.mapHeight = mapHeight;
        this.StartMapGeneration();
    }

    private void StartMapGeneration()
    {
        map = InitializeMap();
    }

    public char[] GetMap()
    {      
        //DebugPrintMap(map);
        return map;
    }

    private char[] InitializeMap()
    {
        char[] map = new char[mapWidth * mapHeight];
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                if(x == 0 && y == 0)
                {
                    map[y * mapWidth + x] = '.';
                }
                else if(DiceRollsUtils.rollD6() > 2)
                {
                    map[y * mapWidth + x] = '.';
                } else
                {
                    map[y * mapWidth + x] = '#';
                }               
            }
        }

        return map;

    }

    private void DebugPrintMap(char[] map)
    {
        for(int y = 0; y < mapHeight; y++)
        {
            string line = "";
            for (int x = 0; x < mapWidth; x++)
            {
                line += map[y * mapWidth + x];
            }
            Debug.Log(line);
        }
    }
}
