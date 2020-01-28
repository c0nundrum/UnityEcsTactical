using Unity.Entities;
using Unity.Mathematics;

[InternalBufferCapacity(8)]
public struct PathBuffer : IBufferElementData
{
    public int2 coordinates;

    // The following implicit conversions are optional, but can be convenient.
    public static implicit operator int2(PathBuffer e)
    {
        return e.coordinates;
    }

    public static implicit operator PathBuffer(int2 e)
    {
        return new PathBuffer { coordinates = e };
    }
}
