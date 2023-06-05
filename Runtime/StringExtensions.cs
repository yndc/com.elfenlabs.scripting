using System.Linq;
using System;

namespace Elfenlabs.Strings
{
    static class Extensions
    {
        public static string AutoTrim(this string code)
        {
            string newline = Environment.NewLine;
            var trimLen = code
                .Split(newline)
                .Skip(1)
                .Min(s => s.Length - s.TrimStart().Length);

            return string.Join(newline,
                code
                .Split(newline)
                .Select(line => line.Substring(Math.Min(line.Length, trimLen))));
        }
    }
}