using System.Text.RegularExpressions;

namespace Octothorpe.Ugity.Editor.Util
{
    public static class StringExtensions
    {
        private static readonly Regex alphanumericPattern = new Regex(@"^[a-zA-Z0-9]+$", RegexOptions.Compiled);
        private static readonly Regex whitespacePattern = new Regex(@"\s+", RegexOptions.Compiled);

        public static bool IsAlphanumeric(this string instance)
        {
            return alphanumericPattern.IsMatch(instance);
        }  
        
        public static string Enquote(this string value, bool multiWordOnly)
        {
            if(multiWordOnly && !whitespacePattern.IsMatch(value)) return value;
            
            if(value.StartsWith("\"") && value.EndsWith("\""))
                return value;
            else
                return string.Concat("\"", value, "\"");
        }

        public static string[] TrimAll(this string[] strings)
        {
            for(int i = 0; i < strings.Length; i++)
            {
                strings[i] = strings[i].Trim();
            }

            return strings;
        }
    }
}
