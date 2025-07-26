
namespace Motely;

unsafe ref partial struct MotelySingleSearchContext
{

    public void Shuffle(string seed, Span<MotelyItem> deck)
    {

        MotelySinglePrngStream stream = CreatePrngStream(seed);
        LuaRandom random = GetNextLuaRandom(ref stream);

        for (int i = deck.Length - 1; i > 0; i--)
        {
            int j = random.RandInt(0, i + 1);
            (deck[i], deck[j]) = (deck[j], deck[i]);
        }

    }
}