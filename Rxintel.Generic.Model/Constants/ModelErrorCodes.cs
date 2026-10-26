using System.ComponentModel;
using System.Reflection;

namespace Rxintel.Generic.Models.Constants
{
    public class ModelErrorCodes
    {
        public static string GetErrorMessage(string errorCode)
        {
            var field =
                typeof(ModelErrorCodes).GetField(errorCode, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                ?? typeof(ModelValidationErrorCodes).GetField(errorCode, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

            var descriptionAttributes = field?.GetCustomAttributes(true);
            if (descriptionAttributes?.FirstOrDefault() is DescriptionAttribute descriptionAttribute)
            {
                return descriptionAttribute.Description;
            }
            return string.Empty;
        }

        public static string GetErrorMessage(string errorCode, params object[] args)
        {
            var messageTemplate = GetErrorMessage(errorCode);
            if (args == null || args.Length == 0)
            {
                return messageTemplate;
            }

            try
            {
                return string.Format(messageTemplate, args);
            }
            catch (FormatException)
            {
                return messageTemplate;
            }
        }
    }
}
