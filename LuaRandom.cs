using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Motely;

public struct LuaRandom
{
    [InlineArray(4)]
    private struct LuaRandomState
    {
        ulong value;
    }
    private LuaRandomState _state;

#if !DEBUG
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public LuaRandom(double seed)
    {
        double d = seed;
        int r = 0x11090601;
        for (int i = 0; i < 4; i++)
        {
            ulong m = 1ul << (r & 255);
            r >>= 8;

            d = d * 3.14159265358979323846 + 2.7182818284590452354;

            ref ulong state = ref _state[i];

            state = Unsafe.As<double, ulong>(ref d);

            if (state < m) state += m;
        }

        ulong z = _state[0];
        for (int i = 0; i < 5; i++)
        {
            z = (((z << 31) ^ z) >> 45) ^ ((z & ulong.MaxValue << 1) << 18);
            z = (((z << 31) ^ z) >> 45) ^ ((z & ulong.MaxValue << 1) << 18);
        }
        _state[0] = z;

        z = _state[1];
        for (int i = 0; i < 5; i++)
        {
            z = (((z << 19) ^ z) >> 30) ^ ((z & ulong.MaxValue << 6) << 28);
            z = (((z << 19) ^ z) >> 30) ^ ((z & ulong.MaxValue << 6) << 28);
        }
        _state[1] = z;

        z = _state[2];
        for (int i = 0; i < 5; i++)
        {
            z = (((z << 24) ^ z) >> 48) ^ ((z & ulong.MaxValue << 9) << 7);
            z = (((z << 24) ^ z) >> 48) ^ ((z & ulong.MaxValue << 9) << 7);
        }
        _state[2] = z;

        z = _state[3];
        for (int i = 0; i < 5; i++)
        {
            z = (((z << 21) ^ z) >> 39) ^ ((z & ulong.MaxValue << 17) << 8);
            z = (((z << 21) ^ z) >> 39) ^ ((z & ulong.MaxValue << 17) << 8);
        }
        _state[3] = z;
    }

#if !DEBUG
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public ulong RandInt()
    {
        ulong r = 0;

        ref ulong z = ref _state[0];
        z = (((z << 31) ^ z) >> 45) ^ ((z & (ulong.MaxValue << 1)) << 18);
        r ^= z;

        z = ref _state[1];
        z = (((z << 19) ^ z) >> 30) ^ ((z & (ulong.MaxValue << 6)) << 28);
        r ^= z;

        z = ref _state[2];
        z = (((z << 24) ^ z) >> 48) ^ ((z & (ulong.MaxValue << 9)) << 7);
        r ^= z;

        z = ref _state[3];
        z = (((z << 21) ^ z) >> 39) ^ ((z & (ulong.MaxValue << 17)) << 8);
        r ^= z;

        return r;
    }

#if !DEBUG
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public ulong RandDblMem()
    {
        return (RandInt() & 4503599627370495) | 4607182418800017408;
    }

#if !DEBUG
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public double Random()
    {
        ulong u = RandDblMem();
        return Unsafe.As<ulong, double>(ref u) - 1.0;
    }

#if !DEBUG
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public int RandInt(int min, int max)
    {
        return (int)(Random() * (max - min + 1)) + min;
    }

#if !DEBUG
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public static ulong RandInt(double seed)
    {
        double d = seed;
        int r = 0x11090601;

        ulong randint = 0;

        ulong m;
        ulong state;

        // state[0]
        m = 1ul << (r & 255);
        r >>= 8;

        d = d * 3.14159265358979323846 + 2.7182818284590452354;

        state = Unsafe.As<double, ulong>(ref d);

        if (state < m) state += m;

        for (int i = 0; i < 5; i++)
        {
            state = (((state << 31) ^ state) >> 45) ^ ((state & ulong.MaxValue << 1) << 18);
            state = (((state << 31) ^ state) >> 45) ^ ((state & ulong.MaxValue << 1) << 18);
        }
        state = (((state << 31) ^ state) >> 45) ^ ((state & ulong.MaxValue << 1) << 18);

        randint ^= state;

        // state[1]
        m = 1ul << (r & 255);
        r >>= 8;

        d = d * 3.14159265358979323846 + 2.7182818284590452354;

        state = Unsafe.As<double, ulong>(ref d);

        if (state < m) state += m;

        for (int i = 0; i < 5; i++)
        {
            state = (((state << 19) ^ state) >> 30) ^ ((state & ulong.MaxValue << 6) << 28);
            state = (((state << 19) ^ state) >> 30) ^ ((state & ulong.MaxValue << 6) << 28);
        }
        state = (((state << 19) ^ state) >> 30) ^ ((state & ulong.MaxValue << 6) << 28);

        randint ^= state;

        // state[2]
        m = 1ul << (r & 255);
        r >>= 8;

        d = d * 3.14159265358979323846 + 2.7182818284590452354;

        state = Unsafe.As<double, ulong>(ref d);

        if (state < m) state += m;

        for (int i = 0; i < 5; i++)
        {
            state = (((state << 24) ^ state) >> 48) ^ ((state & ulong.MaxValue << 9) << 7);
            state = (((state << 24) ^ state) >> 48) ^ ((state & ulong.MaxValue << 9) << 7);
        }
        state = (((state << 24) ^ state) >> 48) ^ ((state & ulong.MaxValue << 9) << 7);

        randint ^= state;

        // state[3]
        m = 1ul << (r & 255);

        d = d * 3.14159265358979323846 + 2.7182818284590452354;

        state = Unsafe.As<double, ulong>(ref d);

        if (state < m) state += m;

        for (int i = 0; i < 5; i++)
        {
            state = (((state << 21) ^ state) >> 39) ^ ((state & ulong.MaxValue << 17) << 8);
            state = (((state << 21) ^ state) >> 39) ^ ((state & ulong.MaxValue << 17) << 8);
        }
        state = (((state << 21) ^ state) >> 39) ^ ((state & ulong.MaxValue << 17) << 8);

        randint ^= state;

        return randint;
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public static ulong RandDblMem(double seed)
    {
        return (RandInt(seed) & 4503599627370495ul) | 4607182418800017408ul;
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public static double Random(double seed)
    {
        ulong u = RandDblMem(seed);
        return Unsafe.As<ulong, double>(ref u) - 1;
    }

#if !DEBUG
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public static int RandInt(double seed, int min, int max)
    {
        return (int)(Random(seed) * (max - min)) + min;
    }
}
