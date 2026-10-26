using System.ComponentModel;

namespace Rxintel.Generic.Enums;

/// <summary>Pharmacy user roles persisted as integer values.</summary>
public enum PharmacyUserRole
{
    [Description("Manager")]
    Manager = 1,
    [Description("Pharmacy admin")]
    PharmacyAdmin = 2,
    [Description("Pharmacist")]
    Pharmacist = 3,
    [Description("Pharmacy Tech")]
    PharmacyTech = 4
}
