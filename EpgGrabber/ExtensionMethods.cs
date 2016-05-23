using System.Globalization;
using System.Text.RegularExpressions;

namespace EpgGrabber
{
    public static class ExtensionMethods
    {
        public static string DecodeNonAsciiCharacters(this string value)
        {
            return Regex.Replace(value, @"\\u(?<Value>[a-zA-Z0-9]{4})", m => ((char)int.Parse(m.Groups["Value"].Value, NumberStyles.HexNumber)).ToString(CultureInfo.InvariantCulture));
        }
    }
}