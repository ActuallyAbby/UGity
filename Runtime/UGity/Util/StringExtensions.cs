using System.Text.RegularExpressions;

namespace Octothorpe.Ugity.Editor.Util
{
    public static class StringExtensions
    {
        private static readonly Regex alphanumericPattern = new Regex(@"^[a-zA-Z0-9]+$", RegexOptions.Compiled);

        public static bool IsAlphanumeric(this string instance)
        {
            return alphanumericPattern.IsMatch(instance);
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
