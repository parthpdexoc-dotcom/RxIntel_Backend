namespace Rxintel.Generic.Enums;

/// <summary>
/// Filters analytics rows by net quantity (ordered minus billed), matching report badge colors.
/// </summary>
public enum NetQuantityFilter
{
    All = 0,
    /// <summary>Green badge: net quantity is zero or positive.</summary>
    Surplus = 1,
    /// <summary>Red badge: net quantity is negative (billed exceeds ordered).</summary>
    Shortage = 2,
}
