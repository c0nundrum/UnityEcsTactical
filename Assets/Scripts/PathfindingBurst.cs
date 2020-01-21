//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using Unity.Entities;
//using Unity.Burst;
//using Unity.Mathematics;
//using Unity.Collections;
//using Unity.Jobs;
//using Unity.Collections.LowLevel.Unsafe;
//using System;

//public struct xy
//{
//    public float x;
//    public float y;
//    public int2 FloorToInt()
//    {
//        return new int2((int)(math.floor(x)), (int)(math.floor(y)));
//    }
//}

//public struct Coordinates
//{
//    public xy xy;
//}

//public struct PathRequest
//{
//    public Coordinates From;
//    public Coordinates To;

//}

//[BurstCompile]
//public struct FindPathJob : IJob
//{
//    public PathRequest Request;
//    public int DimX;
//    public int DimY;
//    public int2 Offset;
//    public int IterationLimit;

//    [ReadOnly] public NativeArray<Neighbour> Neighbours;
//    [ReadOnly] public NativeArray<Cell> Grid;

//    public NativeList<int2> Waypoints;
//    public NativeArray<float> CostSoFar;
//    public NativeArray<int2> CameFrom;
//    public NativeMinHeap OpenSet;

//    public void Execute()
//    {
//        Waypoints.Clear();

//        var start = Request.From.xy.FloorToInt() - Offset;
//        var end = Request.To.xy.FloorToInt() - Offset;

//        FindPath(start, end);
//    }

//    private void FindPath(int2 start, int2 end)
//    {
//        if (start.Equals(end))
//            return;

//        var head = new MinHeapNode(start, H(start, end));
//        OpenSet.Push(head);

//        while (IterationLimit > 0 && OpenSet.HasNext())
//        {
//            var currentIndex = OpenSet.Pop();
//            var current = OpenSet[currentIndex];

//            if (current.Position.Equals(end))
//            {
//                ReconstructPath(start, end);
//                return;
//            }

//            var initialCost = CostSoFar[GetIndex(current.Position)];

//            for (var i = 0; i < Neighbours.Length; i++)
//            {
//                var neigbour = Neighbours[i];
//                var position = current.Position + neigbour.Offset;

//                if (position.x < 0 || position.x >= DimX || position.y < 0 || position.y >= DimY)
//                    continue;

//                var index = GetIndex(position);

//                var cellCost = GetCellCost(currentIndex, index, true);

//                if (float.IsInfinity(cellCost))
//                    continue;

//                var newCost = initialCost + neigbour.Cost * cellCost;
//                var oldCost = CostSoFar[index];

//                if (!(oldCost <= 0) && !(newCost < oldCost))
//                    continue;

//                CostSoFar[index] = newCost;
//                CameFrom[index] = current.Position;

//                var expectedCost = newCost + H(position, end);
//                OpenSet.Push(new MinHeapNode(position, expectedCost));
//            }

//            IterationLimit--;
//        }

//        // If the openset still has a next, means we ran out of iterations (and not path is unobtainable)
//        // So just return the best we've found so far, will finish it later
//        if (OpenSet.HasNext())
//        {
//            var currentIndex = OpenSet.Pop();
//            var current = OpenSet[currentIndex];
//            ReconstructPath(start, current.Position);
//        }
//    }

//    private float GetCellCost(int fromIndex, int toIndex, bool areNeighbours)
//    {
//        var cell = Grid[toIndex];
//        if (cell.Blocked)
//            return float.PositiveInfinity;

//        // TODO HEIGHT ADJUSTMENTS ETC

//        return 1;
//    }

//    private static float H(int2 p0, int2 p1)
//    {
//        var dx = p0.x - p1.x;
//        var dy = p0.y - p1.y;
//        var sqr = dx * dx + dy * dy;
//        return math.sqrt(sqr);
//    }

//    private void ReconstructPath(int2 start, int2 end)
//    {
//        Waypoints.Add(end + Offset);

//        var current = end;
//        do
//        {
//            var previous = CameFrom[GetIndex(current)];
//            current = previous;
//            Waypoints.Add(current + Offset);
//        } while (!current.Equals(start));

//        Waypoints.Reverse();
//    }

//    private int GetIndex(int2 i)
//    {
//        return i.y * DimX + i.x;
//    }
//}

//[NativeContainerSupportsDeallocateOnJobCompletion]
//[NativeContainerSupportsMinMaxWriteRestriction]
//[NativeContainer]
//public unsafe struct NativeMinHeap : IDisposable
//{
//    [NativeDisableUnsafePtrRestriction] private void* m_Buffer;
//    private int m_capacity;
//#if ENABLE_UNITY_COLLECTIONS_CHECKS
//    private AtomicSafetyHandle m_Safety;
//    [NativeSetClassTypeToNullOnSchedule] private DisposeSentinel m_DisposeSentinel;
//#endif
//    private Allocator m_AllocatorLabel;

//    private int m_head;
//    private int m_length;
//    private int m_MinIndex;
//    private int m_MaxIndex;

//    public NativeMinHeap(int capacity, Allocator allocator/*, NativeArrayOptions options = NativeArrayOptions.ClearMemory*/)
//    {
//        Allocate(capacity, allocator, out this);
//        /*if ((options & NativeArrayOptions.ClearMemory) != NativeArrayOptions.ClearMemory)
//            return;
//        UnsafeUtility.MemClear(m_Buffer, (long) m_capacity * UnsafeUtility.SizeOf<MinHeapNode>());*/
//    }

//    private static void Allocate(int capacity, Allocator allocator, out NativeMinHeap nativeMinHeap)
//    {
//        var size = (long)UnsafeUtility.SizeOf<MinHeapNode>() * capacity;
//        if (allocator <= Allocator.None)
//            throw new ArgumentException("Allocator must be Temp, TempJob or Persistent", nameof(allocator));
//        if (capacity < 0)
//            throw new ArgumentOutOfRangeException(nameof(capacity), "Length must be >= 0");
//        if (size > int.MaxValue)
//            throw new ArgumentOutOfRangeException(nameof(capacity),
//                $"Length * sizeof(T) cannot exceed {(object)int.MaxValue} bytes");

//        nativeMinHeap.m_Buffer = UnsafeUtility.Malloc(size, UnsafeUtility.AlignOf<MinHeapNode>(), allocator);
//        nativeMinHeap.m_capacity = capacity;
//        nativeMinHeap.m_AllocatorLabel = allocator;
//        nativeMinHeap.m_MinIndex = 0;
//        nativeMinHeap.m_MaxIndex = capacity - 1;
//        nativeMinHeap.m_head = -1;
//        nativeMinHeap.m_length = 0;

//#if ENABLE_UNITY_COLLECTIONS_CHECKS
//#if UNITY_2018_3_OR_NEWER
//        DisposeSentinel.Create(out nativeMinHeap.m_Safety, out nativeMinHeap.m_DisposeSentinel, 1, allocator); //this was label
//#else
//            DisposeSentinel.Create(out nativeMinHeap.m_Safety, out nativeMinHeap.m_DisposeSentinel, 1);
//#endif
//#endif


//    }

//    public bool HasNext()
//    {
//#if ENABLE_UNITY_COLLECTIONS_CHECKS
//        AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
//#endif
//        return m_head >= 0;
//    }

//    public void Push(MinHeapNode node)
//    {
//#if ENABLE_UNITY_COLLECTIONS_CHECKS
//        if (m_length == m_capacity)
//            throw new IndexOutOfRangeException($"Capacity Reached");
//        AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
//#endif

//        UnsafeUtility.WriteArrayElement(m_Buffer, m_length, node);
//        m_length += 1;

//        if (m_head < 0)
//        {
//            m_head = m_length - 1;
//        }
//        else if (node.ExpectedCost < this[m_head].ExpectedCost)
//        {
//            node.Next = m_head;
//            m_head = m_length - 1;
//        }
//        else
//        {
//            var currentPtr = m_head;
//            var current = this[currentPtr];

//            while (current.Next >= 0 && this[current.Next].ExpectedCost <= node.ExpectedCost)
//            {
//                currentPtr = current.Next;
//                current = this[current.Next];
//            }

//            node.Next = current.Next;
//            current.Next = m_length - 1;
//        }
//    }

//    public int Pop()
//    {
//#if ENABLE_UNITY_COLLECTIONS_CHECKS
//        AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
//#endif
//        var result = m_head;
//        m_head = this[m_head].Next;
//        return result;
//    }

//    public MinHeapNode this[int index]
//    {
//        get
//        {
//#if ENABLE_UNITY_COLLECTIONS_CHECKS
//            if (index < m_MinIndex || index > m_MaxIndex)
//                FailOutOfRangeError(index);
//            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
//#endif

//            return UnsafeUtility.ReadArrayElement<MinHeapNode>(m_Buffer, index);
//        }
//    }

//    public void Clear()
//    {
//        m_head = -1;
//        m_length = 0;
//    }

//    public void Dispose()
//    {
//        if (!UnsafeUtility.IsValidAllocator(m_AllocatorLabel))
//            throw new InvalidOperationException("The NativeArray can not be Disposed because it was not allocated with a valid allocator.");
//#if ENABLE_UNITY_COLLECTIONS_CHECKS
//        DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
//#endif
//        UnsafeUtility.Free(m_Buffer, m_AllocatorLabel);
//        m_Buffer = null;
//        m_capacity = 0;
//    }

//#if ENABLE_UNITY_COLLECTIONS_CHECKS
//    private void FailOutOfRangeError(int index)
//    {
//        if (index < m_capacity && (this.m_MinIndex != 0 || this.m_MaxIndex != m_capacity - 1))
//            throw new IndexOutOfRangeException(
//                $"Index {(object)index} is out of restricted IJobParallelFor range [{(object)this.m_MinIndex}...{(object)this.m_MaxIndex}] in ReadWriteBuffer.\nReadWriteBuffers are restricted to only read & write the element at the job index. You can use double buffering strategies to avoid race conditions due to reading & writing in parallel to the same elements from a job.");
//        throw new IndexOutOfRangeException(
//            $"Index {(object)index} is out of range of '{(object)m_capacity}' Length.");
//    }
//#endif
//}

//public struct MinHeapNode
//{
//    public MinHeapNode(int2 position, float expectedCost)
//    {
//        Position = position;
//        ExpectedCost = expectedCost;
//        Next = -1;
//    }

//    public int2 Position { get; } // TODO to position
//    public float ExpectedCost { get; }
//    public int Next { get; set; }
//}
