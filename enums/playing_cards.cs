namespace Motely;


public enum MotelyPlayingCardRank
{
    Two,
    Three,
    Four,
    Five,
    Six,
    Seven,
    Eight,
    Nine,
    Ten,
    Jack,
    Queen,
    King,
    Ace,
}

public enum MotelyPlayingCardSuit
{
    Club = 0b00 << Motely.PlayingCardSuitOffset,
    Diamond = 0b01 << Motely.PlayingCardSuitOffset,
    Heart = 0b10 << Motely.PlayingCardSuitOffset,
    Spade = 0b11 << Motely.PlayingCardSuitOffset
}

public enum MotelyPlayingCard
{
    C2 = MotelyPlayingCardSuit.Club | MotelyPlayingCardRank.Two,
    C3 = MotelyPlayingCardSuit.Club | MotelyPlayingCardRank.Three,
    C4 = MotelyPlayingCardSuit.Club | MotelyPlayingCardRank.Four,
    C5 = MotelyPlayingCardSuit.Club | MotelyPlayingCardRank.Five,
    C6 = MotelyPlayingCardSuit.Club | MotelyPlayingCardRank.Six,
    C7 = MotelyPlayingCardSuit.Club | MotelyPlayingCardRank.Seven,
    C8 = MotelyPlayingCardSuit.Club | MotelyPlayingCardRank.Eight,
    C9 = MotelyPlayingCardSuit.Club | MotelyPlayingCardRank.Nine,
    C10 = MotelyPlayingCardSuit.Club | MotelyPlayingCardRank.Ten,
    CJ = MotelyPlayingCardSuit.Club | MotelyPlayingCardRank.Jack,
    CQ = MotelyPlayingCardSuit.Club | MotelyPlayingCardRank.Queen,
    CK = MotelyPlayingCardSuit.Club | MotelyPlayingCardRank.King,
    CA = MotelyPlayingCardSuit.Club | MotelyPlayingCardRank.Ace,

    D2 = MotelyPlayingCardSuit.Diamond | MotelyPlayingCardRank.Two,
    D3 = MotelyPlayingCardSuit.Diamond | MotelyPlayingCardRank.Three,
    D4 = MotelyPlayingCardSuit.Diamond | MotelyPlayingCardRank.Four,
    D5 = MotelyPlayingCardSuit.Diamond | MotelyPlayingCardRank.Five,
    D6 = MotelyPlayingCardSuit.Diamond | MotelyPlayingCardRank.Six,
    D7 = MotelyPlayingCardSuit.Diamond | MotelyPlayingCardRank.Seven,
    D8 = MotelyPlayingCardSuit.Diamond | MotelyPlayingCardRank.Eight,
    D9 = MotelyPlayingCardSuit.Diamond | MotelyPlayingCardRank.Nine,
    D10 = MotelyPlayingCardSuit.Diamond | MotelyPlayingCardRank.Ten,
    DJ = MotelyPlayingCardSuit.Diamond | MotelyPlayingCardRank.Jack,
    DQ = MotelyPlayingCardSuit.Diamond | MotelyPlayingCardRank.Queen,
    DK = MotelyPlayingCardSuit.Diamond | MotelyPlayingCardRank.King,
    DA = MotelyPlayingCardSuit.Diamond | MotelyPlayingCardRank.Ace,

    H2 = MotelyPlayingCardSuit.Heart | MotelyPlayingCardRank.Two,
    H3 = MotelyPlayingCardSuit.Heart | MotelyPlayingCardRank.Three,
    H4 = MotelyPlayingCardSuit.Heart | MotelyPlayingCardRank.Four,
    H5 = MotelyPlayingCardSuit.Heart | MotelyPlayingCardRank.Five,
    H6 = MotelyPlayingCardSuit.Heart | MotelyPlayingCardRank.Six,
    H7 = MotelyPlayingCardSuit.Heart | MotelyPlayingCardRank.Seven,
    H8 = MotelyPlayingCardSuit.Heart | MotelyPlayingCardRank.Eight,
    H9 = MotelyPlayingCardSuit.Heart | MotelyPlayingCardRank.Nine,
    H10 = MotelyPlayingCardSuit.Heart | MotelyPlayingCardRank.Ten,
    HJ = MotelyPlayingCardSuit.Heart | MotelyPlayingCardRank.Jack,
    HQ = MotelyPlayingCardSuit.Heart | MotelyPlayingCardRank.Queen,
    HK = MotelyPlayingCardSuit.Heart | MotelyPlayingCardRank.King,
    HA = MotelyPlayingCardSuit.Heart | MotelyPlayingCardRank.Ace,

    S2 = MotelyPlayingCardSuit.Spade | MotelyPlayingCardRank.Two,
    S3 = MotelyPlayingCardSuit.Spade | MotelyPlayingCardRank.Three,
    S4 = MotelyPlayingCardSuit.Spade | MotelyPlayingCardRank.Four,
    S5 = MotelyPlayingCardSuit.Spade | MotelyPlayingCardRank.Five,
    S6 = MotelyPlayingCardSuit.Spade | MotelyPlayingCardRank.Six,
    S7 = MotelyPlayingCardSuit.Spade | MotelyPlayingCardRank.Seven,
    S8 = MotelyPlayingCardSuit.Spade | MotelyPlayingCardRank.Eight,
    S9 = MotelyPlayingCardSuit.Spade | MotelyPlayingCardRank.Nine,
    S10 = MotelyPlayingCardSuit.Spade | MotelyPlayingCardRank.Ten,
    SJ = MotelyPlayingCardSuit.Spade | MotelyPlayingCardRank.Jack,
    SQ = MotelyPlayingCardSuit.Spade | MotelyPlayingCardRank.Queen,
    SK = MotelyPlayingCardSuit.Spade | MotelyPlayingCardRank.King,
    SA = MotelyPlayingCardSuit.Spade | MotelyPlayingCardRank.Ace,
}
