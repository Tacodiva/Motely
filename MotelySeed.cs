using System.Runtime.CompilerServices;

namespace Motely;

[InlineArray(Motely.MaxSeedLength)]
public struct MotelySeedCharacters
{
    public char Character;
}

public readonly struct MotelySeed
{
    public readonly int Length;
    public readonly MotelySeedCharacters Characters;

    public MotelySeed(string seed)
    {
        ArgumentOutOfRangeException.ThrowIfZero(seed.Length, nameof(seed.Length));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(seed.Length, Motely.MaxSeedLength, nameof(seed.Length));

        Length = seed.Length;

        for (int i = 0; i < Length; i++)
        {
            char character = char.ToUpper(seed[i]);

            if (character == '0') character = 'O';

            if (!Motely.SeedDigits.Contains(character))
                throw new ArgumentException($"Illegal character '{seed[i]}' in seed.");

            Characters[i] = character;
        }
    }
}

