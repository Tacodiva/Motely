using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Motely;


public unsafe static class VectorEnum256
{

    public static VectorEnum256<T> Create<T>(T value) where T : unmanaged, Enum
    {
        return new(Vector256.Create(Unsafe.As<T, int>(ref value)));
    }

    public static VectorEnum256<T> Create<T>(Vector256<int> indices, T[] values) where T : unmanaged, Enum
    {
        // Maybe TODO Use _mm512_mask_i32gather_epi32

        T* vector = stackalloc T[Vector256<int>.Count];

        for (int i = 0; i < Vector256<int>.Count; i++)
        {
            vector[i] = values[indices[i]];
        }

        return new(Vector256.Create(
            Unsafe.As<T, int>(ref values[indices[0]]),
            Unsafe.As<T, int>(ref values[indices[1]]),
            Unsafe.As<T, int>(ref values[indices[2]]),
            Unsafe.As<T, int>(ref values[indices[3]]),
            Unsafe.As<T, int>(ref values[indices[4]]),
            Unsafe.As<T, int>(ref values[indices[5]]),
            Unsafe.As<T, int>(ref values[indices[6]]),
            Unsafe.As<T, int>(ref values[indices[7]])
        ));
    }

    public static Vector256<int> Equals<T>(in VectorEnum256<T> a, T b) where T : unmanaged, Enum
    {
        return Vector256.Equals(a.HardwareVector, Vector256.Create(Unsafe.As<T, int>(ref b)));
    }

    public static Vector256<int> Equals<T>(in VectorEnum256<T> a, in VectorEnum256<T> b) where T : unmanaged, Enum
    {
        return Vector256.Equals(a.HardwareVector, b.HardwareVector);
    }

}

public unsafe struct VectorEnum256<T>(Vector256<int> hardwareVector) where T : unmanaged, Enum
{
    public Vector256<int> HardwareVector = hardwareVector;

    static VectorEnum256()
    {
        if (sizeof(T) != 4) throw new ArgumentException($"Size of {nameof(T)} must be 4 bytes.");
    }

    public readonly T this[int i]
    {
        get
        {
            int value = HardwareVector[i];
            return Unsafe.As<int, T>(ref value);
        }
    }

    public override string ToString()
    {
        return $"<{this[0]}, {this[1]}, {this[2]}, {this[3]}, {this[4]}, {this[5]}, {this[6]}, {this[7]}>";
    }
}