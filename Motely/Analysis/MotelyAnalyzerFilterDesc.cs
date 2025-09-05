
using System.ComponentModel;

namespace Motely.Analysis;

/// <summary>
/// Filter descriptor for seed analysis
/// </summary>
public sealed class MotelyAnalyzerFilterDesc() : IMotelySeedFilterDesc<MotelyAnalyzerFilterDesc.AnalyzerFilter>
{
    public MotelySeedAnalysis? LastAnalysis { get; private set; } = null;

    public AnalyzerFilter CreateFilter(ref MotelyFilterCreationContext ctx)
    {
        return new AnalyzerFilter(this);
    }

    public readonly struct AnalyzerFilter(MotelyAnalyzerFilterDesc filterDesc) : IMotelySeedFilter
    {

        public MotelyAnalyzerFilterDesc FilterDesc { get; } = filterDesc;

        public readonly VectorMask Filter(ref MotelyVectorSearchContext ctx)
        {
            return ctx.SearchIndividualSeeds(CheckSeed);
        }

        private ref struct AnteAnalysisState
        {
            public MotelySingleTarotStream ArcanaStream;
            public readonly bool HasArcanaStream => !ArcanaStream.IsNull;
            public MotelySinglePlanetStream CelestialStream;
            public readonly bool HasCelestialStream => !CelestialStream.IsNull;
            public MotelySingleSpectralStream SpectralStream;
            public readonly bool HasSpectralStream => !SpectralStream.IsNull;
            public MotelySingleStandardCardStream StandardStream;
            public readonly bool HasStandardStream => !StandardStream.IsInvalid;
            public MotelySingleJokerStream BuffoonStream;
            public readonly bool HasBuffoonStream => !BuffoonStream.IsNull;
        }

        public readonly bool CheckSeed(ref MotelySingleSearchContext ctx)
        {
            // Create voucher state to track activated vouchers across antes
            MotelyRunState voucherState = new();
            MotelySingleBossStream bossStream = ctx.CreateBossStream();

            List<MotelyAnteAnalysis> antes = [];

            // Analyze each ante
            for (int ante = 1; ante <= 8; ante++)
            {

                AnteAnalysisState state = new()
                {
                    ArcanaStream = default,
                    CelestialStream = default,
                    SpectralStream = default,
                    StandardStream = MotelySingleStandardCardStream.Invalid,
                    BuffoonStream = default
                };

                // Boss
                MotelyBossBlind boss = ctx.GetBossForAnte(ref bossStream, ante, ref voucherState);

                // Voucher - get with state for proper progression
                MotelyVoucher voucher = ctx.GetAnteFirstVoucher(ante, voucherState);

                // TEST: Activate ALL vouchers from ante 1 onwards
                // if (ShouldActivateVoucher(voucher))
                voucherState.ActivateVoucher(voucher);

                // Tags
                MotelySingleTagStream tagStream = ctx.CreateTagStream(ante);

                MotelyTag smallTag = ctx.GetNextTag(ref tagStream);
                MotelyTag bigTag = ctx.GetNextTag(ref tagStream);

                // Shop Queue
                MotelySingleShopItemStream shopStream = ctx.CreateShopItemStream(ante);

                int maxSlots = ante == 1 ? 15 : 50;
                MotelyItem[] shopItems = new MotelyItem[maxSlots];

                for (int i = 0; i < maxSlots; i++)
                {
                    shopItems[i] = ctx.GetNextShopItem(ref shopStream);
                }

                // Packs - Get the actual shop packs (not tag-generated ones)
                // Balatro generates 2 base shop packs, then tags can add more up to 4 in ante 1 or 6 in other antes
                var packStream = ctx.CreateBoosterPackStream(ante);

                int maxPacks = ante == 1 ? 4 : 6;
                MotelyBoosterPackAnalysis[] packs = new MotelyBoosterPackAnalysis[maxPacks];

                // Get all packs up to the maximum
                for (int i = 0; i < maxPacks; i++)
                {
                    MotelyBoosterPack pack = ctx.GetNextBoosterPack(ref packStream);
                    MotelySingleItemSet packContent = GetPackContents(ref ctx, ante, pack, ref state);

                    packs[i] = new(pack, packContent.AsArray());
                }

                antes.Add(new(
                    ante,
                    boss,
                    voucher,
                    smallTag,
                    bigTag,
                    shopItems,
                    packs
                ));
            }

            FilterDesc.LastAnalysis = new(null, antes);

            return false; // Always return false since we're just analyzing
        }

        private static bool ShouldActivateVoucher(MotelyVoucher voucher)
        {
            // Match Immolate's banned vouchers list
            // Magic Trick should be activated (it upgrades to Illusion)
            return voucher != MotelyVoucher.Illusion &&
                   voucher != MotelyVoucher.TarotTycoon &&
                   voucher != MotelyVoucher.TarotMerchant &&
                   voucher != MotelyVoucher.PlanetTycoon &&
                   voucher != MotelyVoucher.PlanetMerchant;
        }


        private static MotelySingleItemSet GetPackContents(
            ref MotelySingleSearchContext ctx, int ante, MotelyBoosterPack pack, ref AnteAnalysisState state
        )
        {
            var packType = pack.GetPackType();
            var packSize = pack.GetPackSize();

            switch (packType)
            {
                case MotelyBoosterPackType.Arcana:

                    if (!state.HasArcanaStream)
                        state.ArcanaStream = ctx.CreateArcanaPackTarotStream(ante);

                    return ctx.GetNextArcanaPackContents(ref state.ArcanaStream, packSize);

                case MotelyBoosterPackType.Celestial:

                    if (!state.HasCelestialStream)
                        state.CelestialStream = ctx.CreateCelestialPackPlanetStream(ante);

                    return ctx.GetNextCelestialPackContents(ref state.CelestialStream, packSize);

                case MotelyBoosterPackType.Spectral:

                    if (!state.HasSpectralStream)
                        state.SpectralStream = ctx.CreateSpectralPackSpectralStream(ante);

                    return ctx.GetNextSpectralPackContents(ref state.SpectralStream, packSize);

                case MotelyBoosterPackType.Buffoon:

                    if (!state.HasBuffoonStream)
                        state.BuffoonStream = ctx.CreateBuffoonPackJokerStream(ante);

                    return ctx.GetNextBuffoonPackContents(ref state.BuffoonStream, packSize);

                case MotelyBoosterPackType.Standard:

                    if (!state.HasStandardStream)
                        state.StandardStream = ctx.CreateStandardPackCardStream(ante);

                    return ctx.GetNextStandardPackContents(ref state.StandardStream, packSize);

                default:
                    throw new InvalidEnumArgumentException();
            }
        }
    }
}
