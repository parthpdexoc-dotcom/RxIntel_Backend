using Rxintel.Generic.Enums;

namespace Rxintel.Generic.Common.Helpers;

public static class WholesalerVawdExpiryHelper
{
    /// <summary>
    /// VAWD date is stored only when VAWD is true. Otherwise it is empty.
    /// </summary>
    public static DateTime? NormalizeVawdDate(bool vawd, DateTime? vawdDate)
        => vawd ? vawdDate : null;

    /// <summary>
    /// VAWD expiry is the day before two years after the VAWD date
    /// (e.g. 01/09/2026 → 31/08/2028). Empty VAWD date → empty expiry.
    /// </summary>
    public static DateTime? FromVawdDate(DateTime? vawdDate)
    {
        if (!vawdDate.HasValue)
            return null;

        return vawdDate.Value.Date.AddYears(2).AddDays(-1);
    }

    /// <summary>
    /// Expiry is calculated from the stored VAWD date. If VAWD is false
    /// or the date is empty, expiry is also empty.
    /// </summary>
    public static DateTime? FromVawd(bool vawd, DateTime? vawdDate)
        => FromVawdDate(NormalizeVawdDate(vawd, vawdDate));

    /// <summary>
    /// Daily job: send only when today is exactly one of the milestone dates.
    /// </summary>
    public static IReadOnlyList<WholesalerExpiryReminderKind> GetDueReminderKinds(
        DateTime today,
        DateTime expiryDate)
    {
        today = today.Date;
        expiryDate = expiryDate.Date;

        var due = new List<WholesalerExpiryReminderKind>();

        if (today == expiryDate.AddMonths(-1))
            due.Add(WholesalerExpiryReminderKind.OneMonthBefore);
        if (today == expiryDate.AddDays(-7))
            due.Add(WholesalerExpiryReminderKind.OneWeekBefore);
        if (today == expiryDate.AddDays(-1))
            due.Add(WholesalerExpiryReminderKind.OneDayBefore);
        if (today == expiryDate.AddMonths(1))
            due.Add(WholesalerExpiryReminderKind.OneMonthAfter);

        return due;
    }
}
