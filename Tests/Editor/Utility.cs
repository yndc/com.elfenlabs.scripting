using System.Linq;
using System;
using System.Text;
using static Elfenlabs.Scripting.Machine;

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

    public static class Utility
    {
        public static unsafe string GetStringFromHeap(int[] heap, int heapIndex)
        {
            fixed (int* ptr = heap)
            {
                return Encoding.UTF8.GetString((byte*)ptr + heapIndex * sizeof(int) + sizeof(int), *(ptr + heapIndex));
            }
        }

        public static unsafe string GetStringFromHeap(Snapshot snapshot, int stackIndex)
        {
            var heapIndex = snapshot.Stack[stackIndex];
            fixed (int* ptr = snapshot.Heap)
            {
                return Encoding.UTF8.GetString((byte*)ptr + heapIndex * sizeof(int) + sizeof(int), *(ptr + heapIndex));
            }
        }
    }
}