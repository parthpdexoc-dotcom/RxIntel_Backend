using System.Text.RegularExpressions;

namespace Rxintel.Generic.Enums;

public enum ImportType
{
    Billing = 1,
    Purchase = 2
}

public static class ImportErrorCategory
{
    public const string ValidationError = "Validation Error";
    public const string ConfigurationError = "Configuration Error";
    public const string FieldMappingError = "Field Mapping Error";
    public const string ColumnMappingError = "Column Mapping Error";
}

public static class ImportFieldLabels
{
    public static string Format(string? fieldName)
    {
        if (string.IsNullOrWhiteSpace(fieldName) || fieldName == "Row")
            return ImportErrorCategory.ValidationError;

        return fieldName switch
        {
            "RXNO" => "RX No",
            "PrimaryInsuranceBinNo" => "Primary Insurance BIN",
            "SecondaryInsuranceBinNo" => "Secondary Insurance BIN",
            _ => Regex.Replace(fieldName, "([a-z])([A-Z])", "$1 $2")
        };
    }
}
