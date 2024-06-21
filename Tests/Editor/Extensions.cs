using System.Linq;
using System;

namespace Elfenlabs.Scripting.Tests
{
    static class Extensions
    {
        public static string NormalizeMultiline(this string code)
        {
            string newline = Environment.NewLine;
            var trimLen = code
                .Split(newline)
                .Where(s => s.TrimStart().Length > 0)
                .Min(s => s.Length - s.TrimStart().Length);
            return string.Join(newline,
                code
                .Split(newline)
                .Select(line => line[Math.Min(line.Length, trimLen)..]));
        }
    }
}