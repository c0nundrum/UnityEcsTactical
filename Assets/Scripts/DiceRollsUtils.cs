using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class DiceRollsUtils
{
    public static int rollD4() {
        return rollDice(4);
    }

    public static int rollD6()
    {
        return rollDice(6);
    }

    private static int rollDice(int faceNum)
    {
        return (int)math.floor(UnityEngine.Random.Range(1, faceNum + 1));
    }
}
