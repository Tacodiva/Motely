
// using System.Runtime.CompilerServices;

// namespace Motely;

// public struct MotelySingleJokerStream
// {
//     public int Ante;
// }

// ref partial struct MotelySingleSearchContext
// {

// #if !DEBUG
//     [MethodImpl(MethodImplOptions.AggressiveInlining)]
// #endif
//     public MotelyItem NextJoker(string source, int ante)
//     {

//         MotelyJokerRarity rarity;

//         switch (source)
//         {
//             case MotelyPrngKeys.JokerSoul:
//                 rarity = MotelyJokerRarity.Legendary;
//                 break;
//             default:


//                 rarity = MotelyJokerRarity.Common;
//                 break;
//         }
//     }
// }