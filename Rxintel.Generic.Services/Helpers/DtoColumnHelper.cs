using System.Reflection;

namespace Rxintel.Generic.Services.Helpers
{
    /// <summary>
    /// Shared DTO column listing for CSV mapping UIs (Billing / Purchase).
    /// </summary>
    public static class DtoColumnHelper
    {
        public static IEnumerable<string> GetPublicPropertyNames(
            Type dtoType,
            IEnumerable<string> excludedNames)
        {
            var excluded = new HashSet<string>(excludedNames, StringComparer.OrdinalIgnoreCase);
            return dtoType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => p.Name)
                .Where(name => !excluded.Contains(name))
                .ToList();
        }
    }
}
