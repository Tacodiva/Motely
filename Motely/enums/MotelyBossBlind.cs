
using System.Runtime.CompilerServices;

namespace Motely;

public enum MotelyBossBlindType
{
    Normal = 0 << Motely.BossTypeOffset,
    Finisher = 1 << Motely.BossTypeOffset
}

internal enum MotelyBossBlindWithoutData
{
    AmberAcorn,
    CeruleanBell,
    CrimsonHeart,
    VerdantLeaf,
    VioletVessel,
    TheArm,
    TheClub,
    TheEye,
    TheFish,
    TheFlint,
    TheGoad,
    TheHead,
    TheHook,
    TheHouse,
    TheManacle,
    TheMark,
    TheMouth,
    TheNeedle,
    TheOx,
    ThePillar,
    ThePlant,
    ThePsychic,
    TheSerpent,
    TheTooth,
    TheWall,
    TheWater,
    TheWheel,
    TheWindow
}

public enum MotelyBossBlind
{
    AmberAcorn = MotelyBossBlindWithoutData.AmberAcorn | MotelyBossBlindType.Finisher,
    CeruleanBell = MotelyBossBlindWithoutData.CeruleanBell | MotelyBossBlindType.Finisher,
    CrimsonHeart = MotelyBossBlindWithoutData.CrimsonHeart | MotelyBossBlindType.Finisher,
    VerdantLeaf = MotelyBossBlindWithoutData.VerdantLeaf | MotelyBossBlindType.Finisher,
    VioletVessel = MotelyBossBlindWithoutData.VioletVessel | MotelyBossBlindType.Finisher,

    TheArm = MotelyBossBlindWithoutData.TheArm | MotelyBossBlindType.Normal | (2 << Motely.BossRequiredAnteOffset),
    TheClub = MotelyBossBlindWithoutData.TheClub | MotelyBossBlindType.Normal,
    TheEye = MotelyBossBlindWithoutData.TheEye | MotelyBossBlindType.Normal | (3 << Motely.BossRequiredAnteOffset),
    TheFish = MotelyBossBlindWithoutData.TheFish | MotelyBossBlindType.Normal | (2 << Motely.BossRequiredAnteOffset),
    TheFlint = MotelyBossBlindWithoutData.TheFlint | MotelyBossBlindType.Normal | (2 << Motely.BossRequiredAnteOffset),
    TheGoad = MotelyBossBlindWithoutData.TheGoad | MotelyBossBlindType.Normal,
    TheHead = MotelyBossBlindWithoutData.TheHead | MotelyBossBlindType.Normal,
    TheHook = MotelyBossBlindWithoutData.TheHook | MotelyBossBlindType.Normal,
    TheHouse = MotelyBossBlindWithoutData.TheHouse | MotelyBossBlindType.Normal | (2 << Motely.BossRequiredAnteOffset),
    TheManacle = MotelyBossBlindWithoutData.TheManacle | MotelyBossBlindType.Normal,
    TheMark = MotelyBossBlindWithoutData.TheMark | MotelyBossBlindType.Normal | (2 << Motely.BossRequiredAnteOffset),
    TheMouth = MotelyBossBlindWithoutData.TheMouth | MotelyBossBlindType.Normal | (2 << Motely.BossRequiredAnteOffset),
    TheNeedle = MotelyBossBlindWithoutData.TheNeedle | MotelyBossBlindType.Normal | (2 << Motely.BossRequiredAnteOffset),
    TheOx = MotelyBossBlindWithoutData.TheOx | MotelyBossBlindType.Normal | (6 << Motely.BossRequiredAnteOffset),
    ThePillar = MotelyBossBlindWithoutData.ThePillar | MotelyBossBlindType.Normal,
    ThePlant = MotelyBossBlindWithoutData.ThePlant | MotelyBossBlindType.Normal | (4 << Motely.BossRequiredAnteOffset),
    ThePsychic = MotelyBossBlindWithoutData.ThePsychic | MotelyBossBlindType.Normal,
    TheSerpent = MotelyBossBlindWithoutData.TheSerpent | MotelyBossBlindType.Normal | (5 << Motely.BossRequiredAnteOffset),
    TheTooth = MotelyBossBlindWithoutData.TheTooth | MotelyBossBlindType.Normal | (3 << Motely.BossRequiredAnteOffset),
    TheWall = MotelyBossBlindWithoutData.TheWall | MotelyBossBlindType.Normal | (2 << Motely.BossRequiredAnteOffset),
    TheWater = MotelyBossBlindWithoutData.TheWater | MotelyBossBlindType.Normal | (2 << Motely.BossRequiredAnteOffset),
    TheWheel = MotelyBossBlindWithoutData.TheWheel | MotelyBossBlindType.Normal | (2 << Motely.BossRequiredAnteOffset),
    TheWindow = MotelyBossBlindWithoutData.TheWindow | MotelyBossBlindType.Normal
}

public static class MotelyBossBlindExt
{
    public static readonly MotelyBossBlind[] FinisherBossBlinds = [
        MotelyBossBlind.AmberAcorn,
        MotelyBossBlind.CeruleanBell,
        MotelyBossBlind.CrimsonHeart,
        MotelyBossBlind.VerdantLeaf,
        MotelyBossBlind.VioletVessel,
    ];

    public static readonly MotelyBossBlind[] NormalBossBlinds = [
        MotelyBossBlind.TheArm,
        MotelyBossBlind.TheClub,
        MotelyBossBlind.TheEye,
        MotelyBossBlind.TheFish,
        MotelyBossBlind.TheFlint,
        MotelyBossBlind.TheGoad,
        MotelyBossBlind.TheHead,
        MotelyBossBlind.TheHook,
        MotelyBossBlind.TheHouse,
        MotelyBossBlind.TheManacle,
        MotelyBossBlind.TheMark,
        MotelyBossBlind.TheMouth,
        MotelyBossBlind.TheNeedle,
        MotelyBossBlind.TheOx,
        MotelyBossBlind.ThePillar,
        MotelyBossBlind.ThePlant,
        MotelyBossBlind.ThePsychic,
        MotelyBossBlind.TheSerpent,
        MotelyBossBlind.TheTooth,
        MotelyBossBlind.TheWall,
        MotelyBossBlind.TheWater,
        MotelyBossBlind.TheWheel,
        MotelyBossBlind.TheWindow,
    ];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetBossIndex(this MotelyBossBlind blind) => ((int)blind) & 0xFF;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetBossMinAnte(this MotelyBossBlind blind) => (((int)blind) & Motely.BossRequiredAnteMask) >> Motely.BossRequiredAnteOffset;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MotelyBossBlindType GetBossType(this MotelyBossBlind blind) => (MotelyBossBlindType)(((int)blind) & Motely.BossTypeMask);

}