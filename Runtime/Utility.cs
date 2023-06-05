using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;

namespace Elfenlabs.Scripting
{
    public static partial class CompilerUtility
    {
        public const int WordSize = 4;

        public static int[] ToIntArray<T>(T value) where T : struct
        {
            var wordLength = UnsafeUtility.SizeOf<T>();
            var array = new int[wordLength];
            unsafe
            {
                UnsafeUtility.CopyStructureToPtr(ref value, UnsafeUtility.AddressOf(ref array[0]));
            }
            return array;
        }

        public static int[] ToIntArray(string value)
        {
            var wordLength = GetWordLength(value.Length * sizeof(char));
            var array = new int[wordLength];
            unsafe
            {
                fixed (char* strPtr = value)
                {
                    fixed (int* arrayPtr = array)
                    {
                        UnsafeUtility.MemCpy(arrayPtr, strPtr, value.Length * sizeof(char));
                    }
                }
            }
            return array;
        }

        public static int GetWordLength<T>() where T : struct
        {
            return GetWordLength(UnsafeUtility.SizeOf<T>());
        }

        public static int GetWordLength(int byteLength)
        {
            return (byteLength + WordSize - 1) / WordSize;
        }

        public static string GenerateSourcePointer(Module module, Location location, int length = 1)
        {
            var iterator = 0;
            var lineStart = -1;
            var lineEnd = -1;
            for (var line = 0; line < location.Line; line++)
            {
                while (iterator < module.Source.Length)
                {
                    var c = module.Source[iterator];
                    if (c == '\n')
                    {
                        if (line < location.Line - 1)
                            break;

                        if (lineStart < 0)
                            lineStart = iterator + 1;
                        else
                        {
                            lineEnd = iterator;
                            goto next;
                        }
                    }
                    iterator++;
                }
            }

        next:

            var lineSource = module.Source.Substring(lineStart, lineEnd - lineStart);
            var linePointer = new string(' ', location.Column - 1) + new string('^', length);
            return lineSource + "\n" + linePointer;
        }
    }
}