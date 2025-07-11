
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Motely;

public struct MotelyWeightedPoolItem<T>(T value, double weight)
    where T : unmanaged, Enum
{
    public T Value = value;
    public double Weight = weight;
}

public unsafe class MotelyWeightedPool<T> : IDisposable
    where T : unmanaged, Enum
{

    private readonly MotelyWeightedPoolItem<T>* _pool;
    public readonly int Count;
    public readonly double WeightSum;

    public MotelyWeightedPool(MotelyWeightedPoolItem<T>[] items)
    {
        Count = items.Length;

        if (Count == 0)
            throw new ArgumentException("Weighted pool must have at least one item.");

        _pool = (MotelyWeightedPoolItem<T>*)Marshal.AllocHGlobal(sizeof(MotelyWeightedPoolItem<T>) * Count);

        double sum = 0;

        for (int i = 0; i < Count; i++)
        {
            _pool[i] = items[i];
            sum += _pool[i].Weight;
        }

        WeightSum = sum;

        // We increase the weight of the last item to make 100% double triple sure something gets picked
        //  before we hit the end of the array.
        _pool[Count - 1].Weight += WeightSum;
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public T Choose(double poll)
    {
        // get_pack common_events.lua
        poll *= WeightSum;

        double weight = 0;
        MotelyWeightedPoolItem<T>* current = _pool;

        for (; ; )
        {
            weight += current->Weight;

            if (weight >= poll)
            {
                return current->Value;
            }

            current += 1;

#if DEBUG
            if (current >= _pool + Count)
                throw new IndexOutOfRangeException();
#endif
        }
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public VectorEnum256<T> Choose(Vector512<double> poll)
    {
        poll *= WeightSum;

        double weight = 0;
        MotelyWeightedPoolItem<T>* current = _pool;
        Vector256<int> finishedMask = Vector256<int>.Zero;

        Vector256<int> values = default;

        for (; ; )
        {
            weight += current->Weight;

            Vector256<int> chosenMask = MotelyVectorUtils.ShrinkDoubleMaskToInt(
                Vector512.GreaterThanOrEqual(Vector512.Create(weight), poll)
            );

            chosenMask &= ~finishedMask;

            values = Vector256.ConditionalSelect(chosenMask, Vector256.Create(*(int*)(&current->Value)), values);

            finishedMask |= chosenMask;

            if (!Vector256.EqualsAny(finishedMask, Vector256.Create(0)))
                return new(values);

            current += 1;

#if DEBUG
            if (current >= _pool + Count)
                throw new IndexOutOfRangeException();
#endif
        }
    }

    public void Dispose()
    {
        Marshal.FreeHGlobal((nint)_pool);
    }
}

public static partial class MotelyWeightedPools { }