
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Motely;

[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly struct MotelyItemVector(Vector256<int> value)
{

    public static int Count => Vector256<int>.Count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<int> Equals(MotelyItemVector a, MotelyItemVector b) =>
        Vector256.Equals(a.Value, b.Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<int> Equals(MotelyItemVector vector, MotelyItem item) =>
        Equals(vector, new MotelyItemVector(Vector256.Create(item.Value)));

    public readonly Vector256<int> Value = value;

    public readonly VectorEnum256<MotelyItemType> Type => new(Vector256.BitwiseAnd(Value, Vector256.Create(Motely.ItemTypeMask)));
    public readonly VectorEnum256<MotelyItemTypeCategory> TypeCategory => new(Vector256.BitwiseAnd(Value, Vector256.Create(Motely.ItemTypeCategoryMask)));
    public readonly VectorEnum256<MotelyItemSeal> Seal => new(Vector256.BitwiseAnd(Value, Vector256.Create(Motely.ItemSealMask)));
    public readonly VectorEnum256<MotelyItemEnhancement> Enhancement => new(Vector256.BitwiseAnd(Value, Vector256.Create(Motely.ItemEnhancementMask)));
    public readonly VectorEnum256<MotelyItemEdition> Edition => new(Vector256.BitwiseAnd(Value, Vector256.Create(Motely.ItemEditionMask)));

    public readonly VectorEnum256<MotelyPlayingCardSuit> PlayingCardSuit => new(Vector256.BitwiseAnd(Value, Vector256.Create(Motely.PlayingCardSuitMask)));
    public readonly VectorEnum256<MotelyPlayingCardRank> PlayingCardRank => new(Vector256.BitwiseAnd(Value, Vector256.Create(Motely.PlayingCardRankMask)));

    public readonly VectorMask IsPerishable => ~Vector256.IsZero(Vector256.BitwiseAnd(Value, Vector256.Create(1 << Motely.PerishableStickerOffset)));
    public readonly VectorMask IsEternal => ~Vector256.IsZero(Vector256.BitwiseAnd(Value, Vector256.Create(1 << Motely.EternalStickerOffset)));
    public readonly VectorMask IsRental => ~Vector256.IsZero(Vector256.BitwiseAnd(Value, Vector256.Create(1 << Motely.RentalStickerOffset)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MotelyItemVector(MotelyItem item) : this(Vector256.Create(item.Value)) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MotelyItemVector AsType(MotelyItemType type)
    {
        return new(Vector256.BitwiseOr(Vector256.BitwiseAnd(Value, Vector256.Create(~Motely.ItemTypeMask)), Vector256.Create((int)type)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MotelyItemVector WithSeal(MotelyItemSeal seal)
    {
        return new(Vector256.BitwiseOr(Vector256.BitwiseAnd(Value, Vector256.Create(~Motely.ItemSealMask)), Vector256.Create((int)seal)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MotelyItemVector WithEnhancement(MotelyItemEnhancement enhancement)
    {
        return new(Vector256.BitwiseOr(Vector256.BitwiseAnd(Value, Vector256.Create(~Motely.ItemEnhancementMask)), Vector256.Create((int)enhancement)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MotelyItemVector WithEdition(MotelyItemEdition edition)
    {
        return new(Vector256.BitwiseOr(Vector256.BitwiseAnd(Value, Vector256.Create(~Motely.ItemEditionMask)), Vector256.Create((int)edition)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MotelyItemVector WithPerishable(bool isPerishable)
    {
        int mask = 1 << Motely.PerishableStickerOffset;
        return new(isPerishable ? Vector256.BitwiseOr(Value, Vector256.Create(mask)) : Vector256.BitwiseAnd(Value, Vector256.Create(~mask)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MotelyItemVector WithEternal(bool isEternal)
    {
        int mask = 1 << Motely.EternalStickerOffset;
        return new(isEternal ? Vector256.BitwiseOr(Value, Vector256.Create(mask)) : Vector256.BitwiseAnd(Value, Vector256.Create(~mask)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MotelyItemVector WithRental(bool isRental)
    {
        int mask = 1 << Motely.RentalStickerOffset;
        return new(isRental ? Vector256.BitwiseOr(Value, Vector256.Create(mask)) : Vector256.BitwiseAnd(Value, Vector256.Create(~mask)));
    }

    public MotelyItem this[int i]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return new(Value[i]);
        }
    }


    public override string ToString()
    {
        return $"<{this[0]}, {this[1]}, {this[2]}, {this[3]}, {this[4]}, {this[5]}, {this[6]}, {this[7]}>";
    }

}