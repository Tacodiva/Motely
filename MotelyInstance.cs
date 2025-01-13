
// namespace Motely;

// public sealed class MotelyInstance(in MotelySeed seed)
// {

//     public readonly MotelySeed Seed = seed;
//     public readonly double SeedHash = Motely.PseudoHash(seed);

//     private readonly Dictionary<string, MotelyPseudoRandomStream> _pseudorandomState = [];

//     public MotelyPseudoRandomStream GetPseudoRandomStream(string key)
//     {
//         if (_pseudorandomState.TryGetValue(key, out MotelyPseudoRandomStream? state)) return state;
//         return _pseudorandomState[key] = new(Motely.PseudoHash(key, Seed), SeedHash);
//     }

//     public double PseudoSeed(string key)
//     {
//         return GetPseudoRandomStream(key).PseudoSeed();
//     }

//     public double PseudoRandom(string key) => Motely.PseudoRandom(PseudoSeed(key));
// }

// public sealed class MotelyPseudoRandomStream(double keyHash, double seedHash)
// {

//     public readonly double SeedHash = seedHash;
//     private double _state = keyHash;

//     public void ProgressState()
//     {
//         _state = Math.Round((_state * 1.72431234 + 2.134453429141) % 1, 13);
//     }

//     public double PseudoSeed()
//     {
//         ProgressState();
//         return (_state + SeedHash) / 2;
//     }

//     public double PseudoRandom() => Motely.PseudoRandom(PseudoSeed());
// }