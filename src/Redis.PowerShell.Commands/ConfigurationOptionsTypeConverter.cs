using System;
using System.Management.Automation;
using StackExchange.Redis;

namespace Redis.PowerShell
{
    public sealed class ConfigurationOptionsTypeConverter : PSTypeConverter
    {
        public override bool CanConvertFrom(object sourceValue, Type destinationType)
        {
            return sourceValue is string && destinationType == typeof(ConfigurationOptions);
        }

        public override bool CanConvertTo(object sourceValue, Type destinationType)
        {
            return false;
        }

        public override object? ConvertFrom(
            object sourceValue,
            Type destinationType,
            IFormatProvider formatProvider,
            bool ignoreCase
        )
        {
            if (sourceValue is null)
            {
                return null;
            }
            else if (sourceValue is string sourceValueString)
            {
                return ConfigurationOptions.Parse(sourceValueString);
            }
            else
            {
                throw new PSInvalidCastException(
                    "Cannot convert from type "
                        + sourceValue.GetType().FullName
                        + " to type "
                        + destinationType.FullName
                );
            }
        }

        public override object ConvertTo(
            object sourceValue,
            Type destinationType,
            IFormatProvider formatProvider,
            bool ignoreCase
        )
        {
            throw new NotImplementedException();
        }
    }
}
