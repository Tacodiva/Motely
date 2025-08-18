namespace Motely;

/// <summary>
/// Specifies the type of tag to filter on
/// </summary>
public enum MotelyTagType
{
    /// <summary>
    /// Any tag (either small blind or big blind)
    /// </summary>
    Any,

    /// <summary>
    /// Small blind tag only
    /// </summary>
    SmallBlind,

    /// <summary>
    /// Big blind tag only
    /// </summary>
    BigBlind
}