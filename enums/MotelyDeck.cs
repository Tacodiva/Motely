
namespace Motely;

public enum MotelyDeck
{
    Red,
    Blue,
    Yellow,
    Green,
    Black,
    Magic,
    Nebula,
    Ghost,
    Abandoned,
    Checkered,
    Zodiac,
    Painted,
    Anaglyph,
    Plasma,
    Erratic
}

public static class MotelyDeckExt
{
    public static MotelyRunState GetDefaultRunState(this MotelyDeck deck)
    {
        MotelyRunState state = default;

        switch (deck)
        {
            case MotelyDeck.Magic:
                state.ActivateVoucher(MotelyVoucher.CrystalBall);
                break;
            case MotelyDeck.Nebula:
                state.ActivateVoucher(MotelyVoucher.Telescope);
                break;
            case MotelyDeck.Zodiac:
                state.ActivateVoucher(MotelyVoucher.TarotMerchant);
                state.ActivateVoucher(MotelyVoucher.PlanetMerchant);
                state.ActivateVoucher(MotelyVoucher.Overstock);
                break;
        }

        return state;
    }
}