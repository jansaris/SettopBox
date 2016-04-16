using System;

namespace WebUi.api
{
    public static class Extensions
    {
        public static bool EqualsIgnoreCase(this string value, string comparison)
        {
            return value.Equals(comparison, StringComparison.OrdinalIgnoreCase);
        }
    }
}